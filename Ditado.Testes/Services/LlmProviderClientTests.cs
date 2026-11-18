using System.Net;
using System.Text.Json;
using Ditado.Aplicacao.Llm;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class LlmProviderClientTests
{
    [Theory]
    [InlineData("Groq", "https://api.groq.com/openai/v1/chat/completions", "max_completion_tokens", "json_schema")]
    [InlineData("Cohere", "https://api.cohere.com/v2/chat", "max_tokens", "json_object")]
    [InlineData("GonkaBroker", "https://proxy.gonkabroker.com/v1/chat/completions", "max_tokens", "json_schema")]
    [InlineData("OpenRouter", "https://openrouter.ai/api/v1/chat/completions", "max_tokens", "json_schema")]
    public async Task ContratoDosQuatroAdaptadores(string provedor, string url, string tokens, string formato)
    {
        const string saida = """{"classificacoes":[{"candidatoId":1,"estado":"correspondente","confianca":null,"evidencia":null}]}""";
        var envelope = provedor == "Cohere"
            ? JsonSerializer.Serialize(new { finish_reason = "COMPLETE", message = new { content = new[] { new { type = "text", text = saida } } } })
            : JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = saida } } } });
        var resultado = await ExecutarAsync(provedor, async (req, ct) =>
        {
            Assert.Equal(url, req.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            using var json = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            var corpo = json.RootElement;
            Assert.Equal("fixture/modelo", corpo.GetProperty("model").GetString());
            Assert.Equal(768, corpo.GetProperty(tokens).GetInt32());
            var responseFormat = corpo.GetProperty("response_format");
            Assert.Equal(formato, responseFormat.GetProperty("type").GetString());
            var contrato = formato == "json_schema" ? responseFormat.GetProperty("json_schema") : responseFormat;
            Assert.Equal("integer", contrato.GetProperty("schema").GetProperty("properties")
                .GetProperty("classificacoes").GetProperty("items").GetProperty("properties")
                .GetProperty("candidatoId").GetProperty("type").GetString());
            if (provedor == "OpenRouter")
            {
                Assert.True(corpo.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
                Assert.False(corpo.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
            }
            return Resposta(200, envelope);
        });
        Assert.Equal(LlmProviderOutcome.Sucesso, resultado.Outcome);
        Assert.Equal(1, Assert.Single(resultado.Output!.Classificacoes).CandidatoId);
    }

    [Theory]
    [InlineData(429, LlmProviderOutcome.Tecnica, LlmMotivo.LimiteProvedor)]
    [InlineData(503, LlmProviderOutcome.Tecnica, LlmMotivo.Indisponivel)]
    [InlineData(408, LlmProviderOutcome.Tecnica, LlmMotivo.Timeout)]
    [InlineData(401, LlmProviderOutcome.Autenticacao, LlmMotivo.Credencial)]
    [InlineData(400, LlmProviderOutcome.Permanente, LlmMotivo.Contrato)]
    public async Task StatusExternoViabilizaSomenteFallbackPrevisto(int status, LlmProviderOutcome desfecho, LlmMotivo motivo)
    {
        var r = await ExecutarAsync("Groq", (_, _) => Task.FromResult(Resposta(status, "mensagem externa não confiável")));
        Assert.Equal((desfecho, motivo, status), (r.Outcome, r.Motivo!.Value, r.HttpStatus!.Value));
    }

    [Fact]
    public async Task LimiteOpenRouterEmEnvelope200ETratadoComoFalhaTecnica()
    {
        var r = await ExecutarAsync("OpenRouter", (_, _) => Task.FromResult(Resposta(200, """{"error":{"code":429}}""")));
        Assert.Equal(LlmMotivo.LimiteProvedor, r.Motivo);
        Assert.Equal(LlmProviderOutcome.Tecnica, r.Outcome);
    }

    [Theory]
    [InlineData("{\"error\":{\"metadata\":{\"limit_source\":\"openrouter_key_limit\"}}}", LlmProviderOutcome.Tecnica)]
    [InlineData("{\"error\":{\"message\":\"forbidden\"}}", LlmProviderOutcome.Autenticacao)]
    public async Task DiferenciaOrcamentoDeProibicaoNoOpenRouter(string corpo, LlmProviderOutcome esperado)
    {
        var r = await ExecutarAsync("OpenRouter", (_, _) => Task.FromResult(Resposta(403, corpo)));
        Assert.Equal(esperado, r.Outcome);
    }

    [Fact]
    public void CadeiaIncompletaNaoHabilitaFila()
    {
        var options = new LlmOptions { Enabled = true, Chain = LlmOptions.Endpoints.Keys
            .Select(p => new LlmProviderOptions { Provider = p, Model = "fixture/modelo" }).ToList() };
        Assert.True(options.PodeEnfileirar);
        options.Chain[1].Model = "";
        Assert.False(options.PodeEnfileirar);
    }

    [Theory]
    [InlineData("{}", "stop", LlmMotivo.SaidaInvalida)]
    [InlineData("json quebrado", "stop", LlmMotivo.SaidaInvalida)]
    [InlineData("{\"classificacoes\":[]}", "length", LlmMotivo.SaidaTruncada)]
    public async Task SaidaInvalidaNaoViraListaVazia(string conteudo, string fim, LlmMotivo motivo)
    {
        var envelope = JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = fim, message = new { content = conteudo } } } });
        var r = await ExecutarAsync("GonkaBroker", (_, _) => Task.FromResult(Resposta(200, envelope)));
        Assert.Equal(LlmProviderOutcome.SaidaInvalida, r.Outcome);
        Assert.Equal(motivo, r.Motivo);
    }

    [Fact]
    public async Task DistingueFalhaDeRedeDeTimeoutECancelamentoDoWorker()
    {
        var rede = await ExecutarAsync("Groq", (_, _) => throw new HttpRequestException());
        Assert.Equal(LlmMotivo.Rede, rede.Motivo);
        var timeout = await ExecutarAsync("Groq", async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException();
        }, new LlmOptions { TimeoutSeconds = 1 });
        Assert.Equal(LlmMotivo.Timeout, timeout.Motivo);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExecutarAsync("Groq",
            (_, ct) => throw new OperationCanceledException(ct), ct: new CancellationToken(true)));
    }

    [Fact]
    public async Task LimiteLocalDeEntradaNaoDisparaHttp()
    {
        var r = await ExecutarAsync("Cohere", (_, _) => throw new InvalidOperationException("HTTP inesperado."),
            new LlmOptions { MaxInputCharacters = 16 });
        Assert.Equal(LlmMotivo.LimiteEntrada, r.Motivo);
        Assert.Equal(LlmProviderOutcome.Permanente, r.Outcome);
    }

    private static Task<LlmProviderResponse> ExecutarAsync(string provedor,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar, LlmOptions? options = null, CancellationToken ct = default)
        => new LlmProviderClient(new FixtureHttpFactory(enviar), Options.Create(options ?? new LlmOptions()), _ => "fixture")
            .ClassificarAsync(new() { Provider = provedor, Model = "fixture/modelo" }, "café", "cafe", [new(1, "AC", "Acentuação", "Diferença de acento")], ct);

    private static HttpResponseMessage Resposta(int status, string texto) =>
        new((HttpStatusCode)status) { Content = new StringContent(texto) };

    private sealed class FixtureHttpFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Handler(enviar));
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) => enviar(req, ct);
    }
}
