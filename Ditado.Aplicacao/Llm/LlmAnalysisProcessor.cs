using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ditado.Aplicacao.Llm;

public sealed class LlmAnalysisProcessor(
    DitadoDbContext context,
    ILlmProviderClient providerClient,
    IOptions<LlmOptions> options)
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.PodeEnfileirar)
            return false;

        var tarefa = await context.LlmAnalisesTarefas
            .Include(x => x.Candidatos)
            .Include(x => x.TentativasProvedor.Where(t => t.EncerradaEm == null))
            .Where(x => x.Estado == LlmAnaliseEstado.Pendente ||
                (x.Estado == LlmAnaliseEstado.Processando && x.IniciadaEm < DateTime.UtcNow.AddMinutes(-5)))
            .OrderBy(x => x.CriadaEm)
            .FirstOrDefaultAsync(cancellationToken);
        if (tarefa is null)
            return false;

        tarefa.Estado = LlmAnaliseEstado.Processando;
        tarefa.IniciadaEm = DateTime.UtcNow;
        foreach (var interrompida in tarefa.TentativasProvedor)
        {
            interrompida.Desfecho = "Interrompida";
            interrompida.Motivo = nameof(LlmMotivo.Interrupcao);
            interrompida.EncerradaEm = tarefa.IniciadaEm;
        }
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
        await ProcessarAsync(tarefa, cancellationToken);
        return true;
    }

    private async Task ProcessarAsync(LlmAnaliseTarefa tarefa, CancellationToken cancellationToken)
    {
        var cadeia = options.Value.Simulated
            ? [new LlmProviderOptions { Provider = "Simulado", Model = "fixture-v1" }]
            : options.Value.Chain;

        var candidatos = tarefa.Candidatos
            .Select(x => new LlmCandidatePrompt(x.Id, x.Codigo, x.Nome, x.Descricao))
            .ToArray();
        var segmento = await context.RespostaSegmentos.AsNoTracking().SingleAsync(x => x.Id == tarefa.RespostaSegmentoId, cancellationToken);

        for (var i = 0; i < cadeia.Count; i++)
        {
            var provider = cadeia[i];
            var tentativa = new LlmTentativaProvedor
            {
                Numero = ++tarefa.Tentativas,
                Provedor = provider.Provider,
                Modelo = provider.Model,
                PromptVersao = tarefa.PromptVersao,
                IniciadaEm = DateTime.UtcNow
            };
            tarefa.TentativasProvedor.Add(tentativa);
            await context.SaveChangesAsync(cancellationToken);

            var resultado = await providerClient.ClassificarAsync(provider, segmento.RespostaEsperada ?? string.Empty,
                segmento.RespostaFornecida, candidatos, cancellationToken);
            tarefa.Provedor = provider.Provider;
            tarefa.Modelo = provider.Model;
            tarefa.Origem = i == 0 ? "Primario" : "Backup";
            tentativa.Desfecho = resultado.Outcome.ToString();
            tentativa.Motivo = resultado.Motivo?.ToString();
            tentativa.HttpStatus = resultado.HttpStatus;
            tentativa.EncerradaEm = DateTime.UtcNow;
            tarefa.UltimoErro = tentativa.Motivo;

            if (resultado.Outcome == LlmProviderOutcome.Tecnica)
            {
                await context.SaveChangesAsync(cancellationToken);
                continue;
            }
            if (resultado.Outcome != LlmProviderOutcome.Sucesso || resultado.Output is null)
            {
                tarefa.Estado = LlmAnaliseEstado.Revisao;
                tarefa.ConcluidaEm = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (!AplicarSaida(tarefa, resultado.Output, out var erro))
            {
                tarefa.Estado = LlmAnaliseEstado.Revisao;
                tarefa.UltimoErro = erro;
                tentativa.Desfecho = nameof(LlmProviderOutcome.SaidaInvalida);
                tentativa.Motivo = nameof(LlmMotivo.SaidaInvalida);
            }
            else
            {
                tarefa.Estado = tarefa.Candidatos.Any(x => x.Estado == LlmCandidatoEstado.Inconclusivo)
                    ? LlmAnaliseEstado.Revisao
                    : LlmAnaliseEstado.Concluida;
                tentativa.Desfecho = tarefa.Estado == LlmAnaliseEstado.Revisao
                    ? "Inconclusiva"
                    : tarefa.Candidatos.Any(x => x.Estado == LlmCandidatoEstado.Correspondente)
                        ? "ComCorrespondencia" : "SemCorrespondencia";
            }
            tarefa.ConcluidaEm = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        FinalizarComFalha(tarefa, tarefa.UltimoErro ?? "Todos os provedores LLM falharam tecnicamente.");
        await context.SaveChangesAsync(cancellationToken);
    }

    public static bool ValidarSaida(LlmAnalysisOutput output, IReadOnlySet<int> candidatos, out string? erro)
    {
        erro = null;
        var retornados = new HashSet<int>();
        if (output.Classificacoes is null || output.Classificacoes.Count > candidatos.Count)
        {
            erro = "Lista de classificações inválida.";
            return false;
        }
        foreach (var item in output.Classificacoes)
        {
            if (item is null || !candidatos.Contains(item.CandidatoId))
            {
                erro = "O provedor retornou candidato fora da seleção.";
                return false;
            }
            if (!retornados.Add(item.CandidatoId))
            {
                erro = "O provedor retornou candidato duplicado.";
                return false;
            }
            if (item.Confianca is < 0 or > 1)
            {
                erro = "A confiança retornada está fora do intervalo permitido.";
                return false;
            }
            if (item.Evidencia?.Length > 2000)
            {
                erro = "A evidência retornada excedeu o limite permitido.";
                return false;
            }
            if (item.Estado is not ("correspondente" or "naoCorrespondente" or "inconclusivo"))
            {
                erro = "Estado de candidato inválido.";
                return false;
            }
        }
        return true;
    }

    private static bool AplicarSaida(LlmAnaliseTarefa tarefa, LlmAnalysisOutput output, out string? erro)
    {
        var candidatos = tarefa.Candidatos.ToDictionary(x => x.Id);
        if (!ValidarSaida(output, candidatos.Keys.ToHashSet(), out erro)) return false;
        var agora = DateTime.UtcNow;
        foreach (var item in output.Classificacoes)
        {
            var candidato = candidatos[item.CandidatoId];
            candidato.Estado = item.Estado switch
            {
                "correspondente" => LlmCandidatoEstado.Correspondente,
                "naoCorrespondente" => LlmCandidatoEstado.NaoCorrespondente,
                "inconclusivo" => LlmCandidatoEstado.Inconclusivo,
                _ => LlmCandidatoEstado.Inconclusivo
            };
            candidato.Confianca = item.Confianca;
            candidato.Evidencia = item.Evidencia;
            candidato.AvaliadoEm = agora;
        }

        foreach (var candidato in tarefa.Candidatos.Where(x => x.Estado == LlmCandidatoEstado.NaoAvaliado))
        {
            candidato.Estado = LlmCandidatoEstado.NaoCorrespondente;
            candidato.AvaliadoEm = agora;
        }
        return true;
    }

    private static void FinalizarComFalha(LlmAnaliseTarefa tarefa, string erro)
    {
        tarefa.Estado = LlmAnaliseEstado.Falha;
        tarefa.UltimoErro = erro;
        tarefa.ConcluidaEm = DateTime.UtcNow;
    }
}
