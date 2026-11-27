using System.Diagnostics;
using Ditado.Aplicacao.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Ditado.Testes.Services;

public sealed class LlmComparacaoModelosTests(ITestOutputHelper saida)
{
    [LlmRealFact]
    [Trait("Categoria", "LlmReal")]
    public async Task CompararTresModelosNaMesmaAmostra()
    {
        var options = new ConfigurationBuilder().AddEnvironmentVariables().Build()
            .GetSection("Llm").Get<LlmOptions>() ?? new();
        Assert.True(options.CadeiaCompleta, "Configure a ordem, os três modelos e os limites.");
        foreach (var (provider, endpoint) in LlmOptions.Endpoints)
            Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(endpoint.Credential)), $"Credencial ausente para {provider}.");

        LlmCandidatePrompt[] candidatos =
        [
            new(1, "AC", "Acentuação", "A grafia difere unicamente pelo acréscimo, supressão ou troca de acento gráfico."),
            new(2, "ES", "Segmentação", "A grafia difere unicamente pela posição ou presença de espaços entre palavras.")
        ];
        (string Esperado, string Fornecido, int[] Rotulos)[] amostra =
        [
            ("café", "cafe", [1]), ("avô", "avó", [1]), ("de novo", "denovo", [2]),
            ("a gente", "agente", [2]), ("casa", "casa", []), ("vela", "bola", []), ("mesa", "", [])
        ];
        var ajustes = Options.Create(options);
        var falhas = 0;
        foreach (var provedor in options.Chain)
        {
            var cliente = new LlmProviderClient(new AtendimentoDireto(), ajustes);
            var validas = 0;
            var exatas = 0;
            var latencias = new List<double>();
            foreach (var (esperado, fornecido, rotulos) in amostra)
            {
                var inicio = Stopwatch.GetTimestamp();
                var entrada = new LlmAttemptPrompt([new(fornecido, [new(1, esperado, fornecido, true)])], candidatos);
                var retorno = await cliente.ClassificarAsync(provedor, entrada, default);
                latencias.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
                if (retorno.Outcome != LlmProviderOutcome.Sucesso || retorno.Output is null ||
                    !LlmAnalysisProcessor.ValidarSaida(retorno.Output, entrada))
                {
                    saida.WriteLine("{0}: desfecho {1}, motivo {2}, HTTP {3}",
                        provedor.Provider, retorno.Outcome, retorno.Motivo, retorno.HttpStatus);
                    falhas++;
                    continue;
                }
                validas++;
                var previstos = retorno.Output.Classificacoes.Where(c => c.Correspondentes.Count > 0)
                    .Select(c => c.CandidatoId).ToHashSet();
                if (previstos.SetEquals(rotulos) && retorno.Output.Classificacoes.All(c => c.Inconclusivas.Count == 0))
                    exatas++;
            }
            latencias.Sort();
            saida.WriteLine("{0} / {1} / {2}: válidas {3}/{4}, acerto exato {5}/{4}, mediana {6:F0} ms",
                provedor.Provider, provedor.Model, LlmOptions.PromptVersion, validas, amostra.Length, exatas, latencias[latencias.Count / 2]);
        }
        Assert.Equal(0, falhas);
    }

    private sealed class AtendimentoDireto : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler { AllowAutoRedirect = false });
    }
}

public sealed class LlmRealFactAttribute : FactAttribute
{
    public LlmRealFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("DITADO_LLM_COMPARAR_SINTETICO") != "1")
            Skip = "Comparação real opcional com DITADO_LLM_COMPARAR_SINTETICO=1.";
    }
}
