using System.Text;
using System.Text.RegularExpressions;
using Ditado.Aplicacao.DTOs.Marketplace;
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
        if (!options.Value.PodeEnfileirar) return false;

        var vencimento = DateTime.UtcNow.AddMinutes(-5);
        var respostaId = await context.LlmAnalisesTarefas
            .Where(t => t.Estado == LlmAnaliseEstado.Pendente ||
                (t.Estado == LlmAnaliseEstado.Processando && t.IniciadaEm < vencimento))
            .Where(t => !context.LlmAnalisesTarefas.Any(outra =>
                outra.RespostaSegmento.RespostaDitadoId == t.RespostaSegmento.RespostaDitadoId &&
                outra.Estado == LlmAnaliseEstado.Processando && outra.IniciadaEm >= vencimento))
            .OrderBy(t => t.CriadaEm).ThenBy(t => t.Id)
            .Select(t => (int?)t.RespostaSegmento.RespostaDitadoId)
            .FirstOrDefaultAsync(cancellationToken);
        if (respostaId is null) return false;

        var resposta = await context.RespostaDitados
            .Include(r => r.Ditado).ThenInclude(d => d.Segmentos)
            .Include(r => r.Ditado).ThenInclude(d => d.DitadoCategorias).ThenInclude(d => d.Categoria)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.AnaliseLlm).ThenInclude(t => t!.Candidatos)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.AnaliseLlm)
                .ThenInclude(t => t!.TentativasProvedor.Where(a => a.EncerradaEm == null))
            .AsSplitQuery().SingleOrDefaultAsync(r => r.Id == respostaId, cancellationToken);
        if (resposta is null) return true;

        var tarefas = resposta.RespostasSegmentos.Select(s => s.AnaliseLlm).OfType<LlmAnaliseTarefa>()
            .Where(t => t.Estado is LlmAnaliseEstado.Pendente or LlmAnaliseEstado.Processando)
            .OrderBy(t => t.Id).ToArray();
        if (tarefas.Length == 0 || tarefas.Any(t => t.Estado == LlmAnaliseEstado.Processando && t.IniciadaEm >= vencimento))
            return true;

        var agora = DateTime.UtcNow;
        foreach (var tarefa in tarefas)
        {
            tarefa.Estado = LlmAnaliseEstado.Processando;
            tarefa.IniciadaEm = agora;
            tarefa.PromptVersao = options.Value.VersaoAtiva;
            foreach (var interrompida in tarefa.TentativasProvedor.Where(a => a.EncerradaEm is null))
            {
                interrompida.Desfecho = "Interrompida";
                interrompida.Motivo = nameof(LlmMotivo.Interrupcao);
                interrompida.EncerradaEm = agora;
            }
        }
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }

        var entrada = ConstruirEntrada(resposta, tarefas);
        if (entrada is null)
        {
            Encerrar(tarefas, LlmAnaliseEstado.Revisao, "Contexto ou seleção de candidatos incompletos.");
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        await ProcessarAsync(tarefas, entrada, cancellationToken);
        return true;
    }

    private static LlmAttemptPrompt? ConstruirEntrada(RespostaDitado resposta, LlmAnaliseTarefa[] tarefas)
    {
        var selecao = tarefas[0].Candidatos.Select(c => c.ErroCatalogoId).ToHashSet();
        if (selecao.Count == 0 || tarefas.Any(t => t.Candidatos.Count != selecao.Count ||
                !selecao.SetEquals(t.Candidatos.Select(c => c.ErroCatalogoId))))
            return null;

        var segmentos = resposta.Ditado.Segmentos.OrderBy(s => s.Ordem).ThenBy(s => s.Id).ToArray();
        var respostas = resposta.RespostasSegmentos.ToLookup(s => s.SegmentoId);
        if (segmentos.Count(s => s.Tipo == TipoSegmento.Lacuna) != resposta.RespostasSegmentos.Count ||
            segmentos.Where(s => s.Tipo == TipoSegmento.Lacuna).Any(s => respostas[s.Id].Count() != 1))
            return null;

        var avaliadas = tarefas.Select(t => t.RespostaSegmentoId).ToHashSet();
        var escrito = new StringBuilder();
        var blocos = new List<LlmBlocoPrompt>();
        var lacunas = new List<LlmGapPrompt>();
        void FecharBloco()
        {
            if (escrito.Length == 0 && lacunas.Count == 0) return;
            blocos.Add(new(escrito.ToString(), lacunas.ToArray()));
            escrito.Clear();
            lacunas.Clear();
        }
        foreach (var segmento in segmentos)
        {
            if (segmento.Tipo != TipoSegmento.Lacuna)
            {
                var inicio = 0;
                foreach (Match fim in Regex.Matches(segmento.Conteudo, @"[.!?…]+(?=\s|$)|\r\n|[\r\n]"))
                {
                    var proximo = fim.Index + fim.Length;
                    escrito.Append(segmento.Conteudo, inicio, proximo - inicio);
                    FecharBloco();
                    inicio = proximo;
                }
                escrito.Append(segmento.Conteudo.AsSpan(inicio));
                continue;
            }

            var respostaLacuna = respostas[segmento.Id].Single();
            var original = respostaLacuna.RespostaAusente ? null : respostaLacuna.RespostaFornecida;
            var gabarito = respostaLacuna.RespostaEsperada ?? segmento.Conteudo;
            var id = respostaLacuna.Id;
            lacunas.Add(new(id, gabarito, original, avaliadas.Contains(id)));
            escrito.Append(original);
            if (Regex.IsMatch(segmento.Conteudo, @"[.!?…]\s*\z")) FecharBloco();
        }
        FecharBloco();

        var congelados = tarefas[0].Candidatos.OrderBy(c => c.ErroCatalogoId).ToArray();
        var candidatos = congelados.Select(c => new LlmCandidatePrompt(c.ErroCatalogoId, c.Codigo, c.Nome, c.Descricao)).ToArray();
        var lacunasRouter = segmentos.Where(s => s.Tipo == TipoSegmento.Lacuna)
            .Select(s => new LlmRouterLacuna(s.Id, respostas[s.Id].Single().RespostaEsperada ?? s.Conteudo)).ToArray();
        var esperados = lacunasRouter.ToDictionary(l => l.SegmentoId, l => l.Esperado);
        return new(blocos, candidatos)
        {
            Router = new(new(resposta.Ditado.Titulo, resposta.Ditado.Descricao,
                string.Concat(segmentos.Select(s => esperados.TryGetValue(s.Id, out var esperado) ? $"[{esperado}]" : s.Conteudo)),
                resposta.Ditado.DitadoCategorias.Select(c => c.Categoria.Nome).OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                lacunasRouter),
                congelados.Select(MapearCandidatoRouter).ToArray(),
                lacunasRouter.Select(l =>
                {
                    var r = respostas[l.SegmentoId].Single();
                    return new LlmRouterResposta(l.SegmentoId, r.RespostaAusente ? null : r.RespostaFornecida);
                }).ToArray()),
            RespostasInternas = lacunasRouter.ToDictionary(l => l.SegmentoId, l => respostas[l.SegmentoId].Single().Id)
        };
    }

    private static LlmRouterCandidato MapearCandidatoRouter(LlmAnaliseCandidato candidato)
    {
        if (candidato.ExemplosAdicionaisJson is null)
            return new(candidato.ErroCatalogoId, candidato.Nome, candidato.Descricao,
                candidato.ExemploEsperado, candidato.ExemploResposta, candidato.ObservacaoPedagogica);

        var exemplos = ExemploErroDto.Ler(candidato.ExemploEsperado, candidato.ExemploResposta,
            candidato.ExemplosAdicionaisJson).Select(exemplo => new[] { exemplo.Esperado, exemplo.Resposta }).ToArray();
        return new(candidato.ErroCatalogoId, candidato.Nome, candidato.Descricao,
            null, null, candidato.ObservacaoPedagogica, exemplos);
    }

    private async Task ProcessarAsync(LlmAnaliseTarefa[] tarefas, LlmAttemptPrompt entrada, CancellationToken cancellationToken)
    {
        var cadeia = options.Value.Simulated
            ? [new LlmProviderOptions { Provider = "Simulado", Model = "fixture-v1" }]
            : new List<LlmProviderOptions> { new() { Provider = LlmOptions.RouterProvider, Model = LlmOptions.RouterModel } };

        // A menor tarefa ainda pendente guarda a trilha da chamada comum à tentativa.
        var primeira = tarefas[0];
        for (var i = 0; i < cadeia.Count; i++)
        {
            var provider = cadeia[i];
            var tentativa = new LlmTentativaProvedor
            {
                Numero = ++primeira.Tentativas, Provedor = provider.Provider, Modelo = provider.Model,
                PromptVersao = options.Value.VersaoAtiva, IniciadaEm = DateTime.UtcNow
            };
            primeira.TentativasProvedor.Add(tentativa);
            await context.SaveChangesAsync(cancellationToken);

            var chamada = entrada with
            {
                Correlacao = new(primeira.RespostaSegmento.RespostaDitadoId, primeira.Id, tentativa.Numero)
            };
            var resultado = await providerClient.ClassificarAsync(provider, chamada, cancellationToken);
            var viaRouter = provider.Provider == LlmOptions.RouterProvider;
            if (viaRouter && resultado.Outcome == LlmProviderOutcome.Sucesso &&
                (resultado.ProvedorEfetivo is null || resultado.ModeloEfetivo is null))
                resultado = resultado with { Outcome = LlmProviderOutcome.SaidaInvalida, Motivo = LlmMotivo.SaidaInvalida };

            tentativa.Provedor = resultado.ProvedorEfetivo ?? provider.Provider;
            tentativa.Modelo = resultado.ModeloEfetivo ?? provider.Model;
            tentativa.TentativasRouter = resultado.TentativasRouter;
            tentativa.Desfecho = resultado.Outcome.ToString();
            tentativa.Motivo = resultado.Motivo?.ToString();
            tentativa.HttpStatus = resultado.HttpStatus;
            tentativa.EncerradaEm = DateTime.UtcNow;
            foreach (var tarefa in tarefas)
            {
                tarefa.Provedor = viaRouter ? resultado.ProvedorEfetivo : provider.Provider;
                tarefa.Modelo = viaRouter ? resultado.ModeloEfetivo : provider.Model;
                tarefa.Origem = viaRouter ? LlmOptions.RouterProvider : i == 0 ? "Primario" : "Backup";
                tarefa.UltimoErro = tentativa.Motivo;
            }

            if (resultado.Outcome == LlmProviderOutcome.Tecnica)
            {
                await context.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (resultado.Outcome != LlmProviderOutcome.Sucesso)
            {
                Encerrar(tarefas, LlmAnaliseEstado.Revisao, tentativa.Motivo);
            }
            else if (resultado.Output is not { } saida || !ValidarSaida(saida, entrada))
            {
                tentativa.Desfecho = nameof(LlmProviderOutcome.SaidaInvalida);
                tentativa.Motivo = nameof(LlmMotivo.SaidaInvalida);
                Encerrar(tarefas, LlmAnaliseEstado.Revisao, "Resposta do provedor fora do contrato da tentativa.");
            }
            else
            {
                AplicarSaida(tarefas, saida);
                tentativa.Desfecho = tarefas.Any(t => t.Estado == LlmAnaliseEstado.Revisao)
                    ? "Inconclusiva"
                    : saida.Classificacoes.Any(c => c.Correspondentes.Count > 0)
                        ? "ComCorrespondencia" : "SemCorrespondencia";
            }

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        Encerrar(tarefas, LlmAnaliseEstado.Falha, primeira.UltimoErro ?? "Todos os provedores LLM falharam tecnicamente.");
        await context.SaveChangesAsync(cancellationToken);
    }

    public static bool ValidarSaida(LlmAnalysisOutput output, LlmAttemptPrompt entrada)
    {
        var lacunas = entrada.Lacunas.Where(l => l.Avaliar).Select(l => l.Id).ToHashSet();
        var candidatos = entrada.Candidatos.Select(c => c.Id).ToHashSet();
        if (output.LacunasAvaliadas is null || output.LacunasAvaliadas.Count != lacunas.Count ||
            !lacunas.SetEquals(output.LacunasAvaliadas) ||
            output.Classificacoes is null || output.Classificacoes.Count != candidatos.Count)
            return false;

        foreach (var grupo in output.Classificacoes)
        {
            if (grupo is null || !candidatos.Remove(grupo.CandidatoId) ||
                grupo.Correspondentes is null || grupo.Inconclusivas is null)
                return false;

            var ids = new HashSet<int>();
            foreach (var item in grupo.Correspondentes.Concat(grupo.Inconclusivas))
                if (item is null || !lacunas.Contains(item.LacunaId) || !ids.Add(item.LacunaId) ||
                    item.Confianca is < 0 or > 1 || item.Evidencia?.Length > 2000)
                    return false;
        }
        return candidatos.Count == 0;
    }

    private static void AplicarSaida(LlmAnaliseTarefa[] tarefas, LlmAnalysisOutput output)
    {
        var porLacuna = tarefas.ToDictionary(t => t.RespostaSegmentoId,
            t => t.Candidatos.ToDictionary(c => c.ErroCatalogoId));
        var agora = DateTime.UtcNow;
        foreach (var candidatos in porLacuna.Values)
            foreach (var candidato in candidatos.Values)
            {
                candidato.Estado = LlmCandidatoEstado.NaoCorrespondente;
                candidato.AvaliadoEm = agora;
            }
        foreach (var grupo in output.Classificacoes)
        {
            foreach (var item in grupo.Correspondentes)
                AplicarAchado(porLacuna[item.LacunaId][grupo.CandidatoId], item, LlmCandidatoEstado.Correspondente);
            foreach (var item in grupo.Inconclusivas)
                AplicarAchado(porLacuna[item.LacunaId][grupo.CandidatoId], item, LlmCandidatoEstado.Inconclusivo);
        }
        foreach (var tarefa in tarefas)
        {
            tarefa.Estado = output.SegmentosInconclusivos.Contains(tarefa.RespostaSegmentoId) ||
                tarefa.Candidatos.Any(c => c.Estado == LlmCandidatoEstado.Inconclusivo)
                ? LlmAnaliseEstado.Revisao : LlmAnaliseEstado.Concluida;
            tarefa.ConcluidaEm = agora;
        }
    }

    private static void AplicarAchado(LlmAnaliseCandidato candidato, LlmGapFinding item, LlmCandidatoEstado estado)
    {
        candidato.Estado = estado;
        candidato.Confianca = item.Confianca;
        candidato.Evidencia = item.Evidencia;
    }

    private static void Encerrar(LlmAnaliseTarefa[] tarefas, LlmAnaliseEstado estado, string? motivo)
    {
        foreach (var tarefa in tarefas)
        {
            tarefa.Estado = estado;
            tarefa.UltimoErro = motivo;
            tarefa.ConcluidaEm = DateTime.UtcNow;
        }
    }
}
