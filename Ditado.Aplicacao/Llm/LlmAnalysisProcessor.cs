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
        if (!options.Value.Enabled)
            return false;

        var tarefa = await context.LlmAnalisesTarefas
            .Include(x => x.Candidatos)
            .Where(x => x.Estado == LlmAnaliseEstado.Pendente ||
                (x.Estado == LlmAnaliseEstado.Processando && x.IniciadaEm < DateTime.UtcNow.AddMinutes(-5)))
            .OrderBy(x => x.CriadaEm)
            .FirstOrDefaultAsync(cancellationToken);
        if (tarefa is null)
            return false;

        tarefa.Estado = LlmAnaliseEstado.Processando;
        tarefa.IniciadaEm = DateTime.UtcNow;
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
        var cadeia = ResolverCadeia();
        if (cadeia.Count == 0)
        {
            FinalizarComFalha(tarefa, "Nenhum provedor LLM configurado.");
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var candidatos = tarefa.Candidatos
            .Select(x => new LlmCandidatePrompt(x.Id, x.Codigo, x.Nome, x.Descricao))
            .ToArray();
        var segmento = await context.RespostaSegmentos.AsNoTracking().SingleAsync(x => x.Id == tarefa.RespostaSegmentoId, cancellationToken);

        for (var i = 0; i < cadeia.Count; i++)
        {
            var provider = cadeia[i];
            var resultado = await providerClient.ClassificarAsync(provider, segmento.RespostaEsperada ?? string.Empty,
                segmento.RespostaFornecida, candidatos, cancellationToken);
            tarefa.Tentativas++;
            tarefa.Provedor = provider.Provider;
            tarefa.Modelo = provider.Model;
            tarefa.Origem = i == 0 ? "Primario" : "Backup";
            tarefa.UltimoErro = resultado.Error;

            if (resultado.Outcome == LlmProviderOutcome.Tecnica)
                continue;
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
            }
            else
            {
                tarefa.Estado = tarefa.Candidatos.Any(x => x.Estado == LlmCandidatoEstado.Inconclusivo)
                    ? LlmAnaliseEstado.Revisao
                    : LlmAnaliseEstado.Concluida;
            }
            tarefa.ConcluidaEm = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        FinalizarComFalha(tarefa, tarefa.UltimoErro ?? "Todos os provedores LLM falharam tecnicamente.");
        await context.SaveChangesAsync(cancellationToken);
    }

    private List<LlmProviderOptions> ResolverCadeia()
    {
        var configurados = options.Value.Providers;
        if (options.Value.Simulated)
            return [new() { Provider = "Simulado", Model = "fixture-v1" }];

        return options.Value.Chain
            .Where(x => !string.IsNullOrWhiteSpace(x.Provider))
            .Select(x => configurados.TryGetValue(x.Provider, out var provider)
                ? new LlmProviderOptions
                {
                    Provider = x.Provider,
                    Model = string.IsNullOrWhiteSpace(x.Model) ? provider.Model : x.Model,
                    BaseUrl = provider.BaseUrl,
                    ApiKeyEnvironmentVariable = provider.ApiKeyEnvironmentVariable
                }
                : new LlmProviderOptions { Provider = x.Provider, Model = x.Model })
            .Where(x => !string.IsNullOrWhiteSpace(x.Model) && x.Model.Length <= 200 &&
                (x.Provider is "Groq" or "Cohere" or "GonkaBroker" or "OpenRouter"))
            .DistinctBy(x => x.Provider)
            .ToList();
    }

    private static bool AplicarSaida(LlmAnaliseTarefa tarefa, LlmAnalysisOutput output, out string? erro)
    {
        erro = null;
        var candidatos = tarefa.Candidatos.ToDictionary(x => x.Id);
        var retornados = new HashSet<int>();
        if (output.Classificacoes is null || output.Classificacoes.Count > candidatos.Count)
        {
            erro = "Lista de classificações inválida.";
            return false;
        }
        foreach (var item in output.Classificacoes)
        {
            if (item is null || !candidatos.ContainsKey(item.CandidatoId))
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

        foreach (var candidato in tarefa.Candidatos.Where(x => !retornados.Contains(x.Id)))
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
