using Ditado.Aplicacao.Llm;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class LlmAnalysisProcessorTests
{
    [Theory]
    [InlineData("limite", "Groq,OpenRouter", LlmAnaliseEstado.Concluida)]
    [InlineData("vazio", "Groq", LlmAnaliseEstado.Concluida)]
    [InlineData("fora", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("duplicado", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("estado", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("confiança", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("permanente", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("malformado", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("nulo", "Groq", LlmAnaliseEstado.Revisao)]
    public async Task RetomaTarefaEValidaSaidaAntesDeGravar(string caso, string ordem, LlmAnaliseEstado estado)
    {
        await using var context = new DitadoDbContext(new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var tarefa = new LlmAnaliseTarefa
        {
            Estado = LlmAnaliseEstado.Processando, IniciadaEm = DateTime.UtcNow.AddMinutes(-6),
            PromptVersao = LlmOptions.PromptVersion,
            RespostaSegmento = new RespostaSegmento
            {
                RespostaEsperada = "café", RespostaFornecida = "cafe", MetodoClassificacao = MetodoClassificacao.Codigo,
                TiposErro = [new() { TipoErro = TipoErro.Acentuacao }]
            },
            Candidatos = [new() { ErroCatalogoId = 31 }, new() { ErroCatalogoId = 32 }]
        };
        context.Add(tarefa);
        await context.SaveChangesAsync();
        var fixture = new Fixture(caso);
        var processor = new LlmAnalysisProcessor(context, fixture, Options.Create(new LlmOptions
        {
            Enabled = true,
            Chain = [new() { Provider = "Cohere" }, new() { Provider = "Groq", Model = "fixture" }, new() { Provider = "OpenRouter", Model = "fixture/backup" }]
        }));
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.False(await processor.ProcessNextAsync(default));
        Assert.Equal(ordem, string.Join(",", fixture.Chamadas));
        context.ChangeTracker.Clear();
        var persistida = await context.LlmAnalisesTarefas.Include(t => t.Candidatos)
            .Include(t => t.RespostaSegmento).ThenInclude(s => s.TiposErro).SingleAsync();
        Assert.Equal(estado, persistida.Estado);
        var esperado = caso switch
        {
            "limite" => LlmCandidatoEstado.Correspondente,
            "vazio" => LlmCandidatoEstado.NaoCorrespondente,
            _ => LlmCandidatoEstado.NaoAvaliado
        };
        Assert.Equal(esperado, persistida.Candidatos.OrderBy(c => c.Id).First().Estado);
        Assert.Equal(TipoErro.Acentuacao, Assert.Single(persistida.RespostaSegmento.TiposErro).TipoErro);
        Assert.Equal("cafe", persistida.RespostaSegmento.RespostaFornecida);
    }

    private sealed class Fixture(string caso) : ILlmProviderClient
    {
        public List<string> Chamadas { get; } = [];

        public Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, string esperado, string fornecido,
            IReadOnlyList<LlmCandidatePrompt> candidatos, CancellationToken cancellationToken)
        {
            Chamadas.Add(provider.Provider);
            var primeiro = new LlmClassificationOutput { CandidatoId = candidatos[0].Id, Estado = "correspondente" };
            var segundo = new LlmClassificationOutput { CandidatoId = candidatos[1].Id, Estado = "correspondente" };
            if (caso == "fora") segundo.CandidatoId = int.MaxValue;
            if (caso == "duplicado") segundo.CandidatoId = primeiro.CandidatoId;
            if (caso == "estado") segundo.Estado = "inventado";
            if (caso == "confiança") segundo.Confianca = 2;
            return Task.FromResult(caso switch
            {
                "permanente" => new LlmProviderResponse(LlmProviderOutcome.Permanente),
                "malformado" => new LlmProviderResponse(LlmProviderOutcome.SaidaInvalida),
                "nulo" => new LlmProviderResponse(LlmProviderOutcome.Sucesso, new() { Classificacoes = null! }),
                "limite" when provider.Provider == "Groq" => new(LlmProviderOutcome.Tecnica, Error: "HTTP 429"),
                _ => new(LlmProviderOutcome.Sucesso, new() { Classificacoes = caso == "vazio" ? [] : [primeiro, segundo] })
            });
        }
    }
}
