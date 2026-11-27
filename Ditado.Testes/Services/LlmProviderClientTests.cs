using System.Net;
using System.Text.Json;
using Ditado.Aplicacao.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class LlmProviderClientTests
{
    [Theory]
    [InlineData("ok", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.Sucesso)]
    [InlineData("ok", "stop", "beta/extra/modelo:free", false, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("ok", "stop", "auto", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("ok", "length", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("{", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("""{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[1]}]}""", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("""{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[1]},{"segmentoId":102,"candidatosCorrespondentes":[]}]}""", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("""{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[]},{"segmentoId":101,"candidatosCorrespondentes":[]}]}""", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("""{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[2]},{"segmentoId":103,"candidatosCorrespondentes":[]}]}""", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    [InlineData("""{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[1,1]},{"segmentoId":103,"candidatosCorrespondentes":[]}]}""", "stop", "beta/extra/modelo:free", true, LlmProviderOutcome.SaidaInvalida)]
    public async Task RouterValidaIdentidadeEfetivaEEnvelope(
        string conteudo, string fim, string modelo, bool incluirIdentidade, LlmProviderOutcome desfecho)
    {
        var saida = conteudo == "ok"
            ? """{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[]},{"segmentoId":103,"candidatosCorrespondentes":[1],"inconclusivo":true}]}"""
            : conteudo;
        var chamadas = 0;
        var resultado = await ExecutarAsync(LlmOptions.RouterProvider, async (req, ct) =>
        {
            chamadas++;
            Assert.Equal("http://localhost:4000/v1/chat/completions", req.RequestUri!.AbsoluteUri);
            using var payload = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            Assert.Equal("auto", payload.RootElement.GetProperty("model").GetString());
            Assert.Equal("json_object", payload.RootElement.GetProperty("response_format").GetProperty("type").GetString());
            var response = Resposta(200, JsonSerializer.Serialize(new
            {
                model = modelo,
                choices = new[] { new { finish_reason = fim, message = new { content = saida } } }
            }));
            response.Headers.Add("x-free-llm-attempts", "2");
            if (incluirIdentidade)
            {
                response.Headers.Add("x-free-llm-provider", "beta");
                response.Headers.Add("x-free-llm-model", "extra/modelo:free");
            }
            return response;
        }, new LlmOptions { RouterUrl = "http://localhost:4000/v1" });
        Assert.Equal(1, chamadas);
        Assert.Equal(desfecho, resultado.Outcome);
        Assert.Equal(2, resultado.TentativasRouter);
        Assert.Equal(200, resultado.HttpStatus);
        if (desfecho == LlmProviderOutcome.Sucesso)
        {
            Assert.Equal("beta", resultado.ProvedorEfetivo);
            Assert.Equal("extra/modelo:free", resultado.ModeloEfetivo);
            Assert.Contains(2, resultado.Output!.SegmentosInconclusivos);
            Assert.Equal(2, Assert.Single(Assert.Single(resultado.Output.Classificacoes).Correspondentes).LacunaId);
        }
        else
            Assert.Null(resultado.Output);
    }

    [Theory]
    [InlineData(0, "stop", false, null)]
    [InlineData(768, "stop", false, null)]
    [InlineData(0, "length", false, LlmMotivo.SaidaTruncada)]
    [InlineData(0, "MAX_TOKENS", false, LlmMotivo.SaidaTruncada)]
    [InlineData(0, "stop", true, LlmMotivo.SaidaInvalida)]
    public async Task RouterTrataZeroComoAusenciaDeLimiteNoPayload(int tokens, string fim,
        bool excedeBuffer, LlmMotivo? motivo)
    {
        var options = new LlmOptions { Enabled = true, RouterUrl = "http://localhost:4000/v1", MaxOutputTokens = tokens };
        Assert.True(options.PodeEnfileirar);
        var resultado = await ExecutarAsync(LlmOptions.RouterProvider, async (req, ct) =>
        {
            using var payload = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            Assert.False(payload.RootElement.TryGetProperty("max_completion_tokens", out _));
            Assert.Equal(tokens > 0, payload.RootElement.TryGetProperty("max_tokens", out var limite));
            if (tokens > 0)
                Assert.Equal(tokens, limite.GetInt32());

            const string saida = """{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[]},{"segmentoId":103,"candidatosCorrespondentes":[]}]}""";
            var corpo = JsonSerializer.Serialize(new
            {
                model = "fixture/modelo",
                choices = new[] { new { finish_reason = fim, message = new { content = saida } } }
            });
            var response = Resposta(200, excedeBuffer ? corpo + new string(' ', 100_001) : corpo);
            response.Headers.Add("x-free-llm-provider", "fixture");
            response.Headers.Add("x-free-llm-model", "modelo");
            return response;
        }, options);

        Assert.Equal(motivo is null ? LlmProviderOutcome.Sucesso : LlmProviderOutcome.SaidaInvalida, resultado.Outcome);
        if (motivo is not null)
            Assert.Equal(motivo, resultado.Motivo);
    }

    [Theory]
    [InlineData(false, false, 200)]
    [InlineData(true, false, 200)]
    [InlineData(true, true, 200)]
    [InlineData(true, false, 503)]
    [InlineData(true, false, 418)]
    public async Task RouterDiagnosticoOptInComCorrelacaoESigilo(bool habilitado, bool ecoNaEntrada, int status)
    {
        const string chave = "credencial-sintetica-router";
        const string segredoUpstream = "credencial-sintetica-upstream";
        var logs = new CapturaLog();
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["GROQ_API_KEY"] = segredoUpstream }).Build();
        var diagnostico = new LlmRouterDiagnostics(logs, configuracao);
        const string saida = """{"segmentos":[{"segmentoId":101,"candidatosCorrespondentes":[]},{"segmentoId":103,"candidatosCorrespondentes":[1]}]}""";
        var resultado = await ExecutarAsync(LlmOptions.RouterProvider, async (req, ct) =>
        {
            Assert.Equal(chave, req.Headers.Authorization?.Parameter);
            var carga = await req.Content!.ReadAsStringAsync(ct);
            Assert.DoesNotContain("correlacao", carga, StringComparison.OrdinalIgnoreCase);
            var response = Resposta(status, status == 418 ? "corpo inválido: " + chave : JsonSerializer.Serialize(new
            {
                model = "beta/alternativo",
                choices = new[] { new { finish_reason = "stop", message = new { content = saida } } },
                Authorization = $"Bearer {chave}",
                usage = new { total_tokens = 19 },
                upstream = new
                {
                    mode = "fallback",
                    observacao = segredoUpstream,
                    detalhes = JsonSerializer.Serialize(new { ativo = true, api_key = "outra-credencial-sintetica" })
                }
            }));
            response.Headers.Add("x-free-llm-provider", "beta");
            response.Headers.Add("x-free-llm-model", "alternativo");
            response.Headers.Add("x-free-llm-attempts", "3");
            return response;
        }, new LlmOptions { RouterUrl = "http://localhost:4000/v1", RouterLogBodies = habilitado },
            diagnostico: diagnostico, chave: chave, titulo: ecoNaEntrada ? chave : "Ditado sintético");

        var esperado = status switch
        {
            200 => LlmProviderOutcome.Sucesso,
            503 => LlmProviderOutcome.Tecnica,
            _ => LlmProviderOutcome.Permanente
        };
        Assert.Equal(esperado, resultado.Outcome);
        if (!habilitado)
        {
            Assert.Empty(logs.Eventos);
            return;
        }

        var campos = Assert.Single(logs.Eventos);
        Assert.IsType<Guid>(campos["RequestId"]);
        Assert.Equal(42, campos["RespostaDitadoId"]);
        Assert.Equal(73, campos["LlmAnaliseTarefaId"]);
        Assert.Equal(2, campos["Tentativa"]);
        Assert.Equal(status, campos["StatusHttp"]);
        Assert.True(Assert.IsType<long>(campos["LatenciaMs"]) >= 0);
        Assert.Equal(3, campos["TentativasRouter"]);
        Assert.Equal(LlmOptions.RouterPromptVersion, campos["PromptVersao"]);
        Assert.Equal(resultado.Outcome.ToString(), campos["Desfecho"]);
        Assert.Equal(resultado.Motivo?.ToString(), campos["Motivo"]);
        var payload = Assert.IsType<string>(campos["Payload"]);
        var resposta = Assert.IsType<string>(campos["Resposta"]);
        using var entrada = JsonDocument.Parse(payload);
        using var lote = JsonDocument.Parse(entrada.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
        var ditado = lote.RootElement.GetProperty("ditado");
        Assert.Equal(ecoNaEntrada ? "[omitido por segurança]" : "Ditado sintético", ditado.GetProperty("titulo").GetString());
        Assert.Equal("[Os alunos] [chegaram].", ditado.GetProperty("textoComMarcacoes").GetString());
        if (status == 200)
        {
            Assert.Equal("beta", campos["Provedor"]);
            Assert.Equal("alternativo", campos["Modelo"]);
            Assert.Contains("candidatosCorrespondentes", resposta);
        }
        if (status != 418)
        {
            using var envelope = JsonDocument.Parse(resposta);
            Assert.Equal(19, envelope.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32());
            var upstream = envelope.RootElement.GetProperty("upstream");
            Assert.Equal("fallback", upstream.GetProperty("mode").GetString());
            Assert.Equal("[omitido por segurança]", upstream.GetProperty("observacao").GetString());
            using var detalhes = JsonDocument.Parse(upstream.GetProperty("detalhes").GetString()!);
            Assert.True(detalhes.RootElement.GetProperty("ativo").GetBoolean());
            Assert.False(detalhes.RootElement.TryGetProperty("api_key", out _));
            Assert.DoesNotContain("outra-credencial-sintetica", resposta);
        }
        else Assert.Equal("null", resposta);
        Assert.All(campos.Values.OfType<string>(), valor =>
        {
            Assert.DoesNotContain(chave, valor);
            Assert.DoesNotContain(segredoUpstream, valor);
            Assert.DoesNotContain("Authorization", valor, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData("Groq", "https://api.groq.com/openai/v1/chat/completions", "max_completion_tokens", "json_schema")]
    [InlineData("Cohere", "https://api.cohere.com/v2/chat", "max_tokens", "json_object")]
    [InlineData("OpenRouter", "https://openrouter.ai/api/v1/chat/completions", "max_tokens", "json_schema")]
    public async Task ContratoDosTresAdaptadores(string provedor, string url, string tokens, string formato)
    {
        const string saida = """{"lacunasAvaliadas":[1,2],"classificacoes":[{"candidatoId":1,"correspondentes":[{"lacunaId":2,"confianca":null,"evidencia":null}],"inconclusivas":[]}]}""";
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
            using var amostra = JsonDocument.Parse(corpo.GetProperty("messages")[1].GetProperty("content").GetString()!);
            Assert.False(amostra.RootElement.TryGetProperty("lacunas", out _));
            var bloco = amostra.RootElement.GetProperty("blocos")[0];
            Assert.Equal("O aluno chegaram.", bloco.GetProperty("textoEscrito").GetString());
            var lacunas = bloco.GetProperty("lacunas");
            Assert.Equal(2, lacunas.GetArrayLength());
            Assert.Equal(2, lacunas[1].GetProperty("id").GetInt32());
            Assert.Equal("chegaram", lacunas[1].GetProperty("escrito").GetString());
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
        Assert.Equal(200, resultado.HttpStatus);
        Assert.Equal(1, Assert.Single(resultado.Output!.Classificacoes).CandidatoId);
        Assert.Equal(2, Assert.Single(resultado.Output!.Classificacoes[0].Correspondentes).LacunaId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CohereLeSomenteOsBlocosDeTextoNaOrdem(bool incluirTexto)
    {
        var blocos = new List<object> { new { type = "thinking", thinking = "Rascunho do provedor." } };
        if (incluirTexto)
        {
            blocos.Add(new { type = "text", text = """{"lacunasAvaliadas":[1,2],""" });
            blocos.Add(new { type = "text", text = "\"classificacoes\":[{\"candidatoId\":1,\"correspondentes\":[],\"inconclusivas\":[]}]}" });
        }
        var envelope = JsonSerializer.Serialize(new { finish_reason = "COMPLETE", message = new { content = blocos } });
        var resultado = await ExecutarAsync("Cohere", (_, _) => Task.FromResult(Resposta(200, envelope)));

        if (incluirTexto)
        {
            Assert.Equal(LlmProviderOutcome.Sucesso, resultado.Outcome);
            Assert.Equal(200, resultado.HttpStatus);
            Assert.Equal(new[] { 1, 2 }, resultado.Output!.LacunasAvaliadas);
            Assert.Equal(1, Assert.Single(resultado.Output.Classificacoes).CandidatoId);
        }
        else
        {
            Assert.Equal(LlmProviderOutcome.SaidaInvalida, resultado.Outcome);
            Assert.Equal(LlmMotivo.SaidaInvalida, resultado.Motivo);
            Assert.Null(resultado.Output);
        }
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
        Assert.False(options.PodeEnfileirar);
        options.Chain[1].Model = "";
        Assert.False(options.PodeEnfileirar);
    }

    [Fact]
    public async Task ProvedorRemovidoInvalidaCadeiaENaoDisparaHttp()
    {
        var provedorRemovido = new LlmProviderOptions { Provider = "GonkaBroker", Model = "fixture" };
        var options = new LlmOptions
        {
            Enabled = true,
            Chain = [new() { Provider = "Groq", Model = "fixture" }, new() { Provider = "Cohere", Model = "fixture" },
                new() { Provider = "OpenRouter", Model = "fixture/modelo" }]
        };
        Assert.False(options.PodeEnfileirar);
        options.Chain.Add(provedorRemovido);
        Assert.False(options.PodeEnfileirar);
        options.Chain.RemoveAt(2);
        Assert.False(options.PodeEnfileirar);

        var resultado = await ExecutarAsync(provedorRemovido.Provider,
            (_, _) => throw new InvalidOperationException("HTTP inesperado."));
        Assert.Equal(LlmProviderOutcome.Permanente, resultado.Outcome);
        Assert.Equal(LlmMotivo.Configuracao, resultado.Motivo);
    }

    [Theory]
    [InlineData("{}", "stop", LlmMotivo.SaidaInvalida)]
    [InlineData("json quebrado", "stop", LlmMotivo.SaidaInvalida)]
    [InlineData("{\"classificacoes\":[]}", "length", LlmMotivo.SaidaTruncada)]
    public async Task SaidaInvalidaNaoViraListaVazia(string conteudo, string fim, LlmMotivo motivo)
    {
        var envelope = JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = fim, message = new { content = conteudo } } } });
        var r = await ExecutarAsync("Groq", (_, _) => Task.FromResult(Resposta(200, envelope)));
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
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar, LlmOptions? options = null, CancellationToken ct = default,
        LlmRouterDiagnostics? diagnostico = null, string chave = "fixture", string titulo = "Fixture")
        => new LlmProviderClient(new FixtureHttpFactory(enviar), Options.Create(options ?? new LlmOptions()), _ => chave, diagnostico)
            .ClassificarAsync(new() { Provider = provedor, Model = provedor == LlmOptions.RouterProvider ? LlmOptions.RouterModel : "fixture/modelo" },
                new([new("O aluno chegaram.",
                    [new(1, "Os alunos", "O aluno", true), new(2, "chegaram", "chegaram", true)])],
                    [new(1, "CV", "Concordância verbal", "Compare sujeito e verbo escritos.")])
                {
                    Correlacao = new(42, 73, 2),
                    Router = new(new(titulo, null, "[Os alunos] [chegaram].", [],
                        [new(101, "Os alunos"), new(103, "chegaram")]),
                        [new(1, "Concordância verbal", "Compare sujeito e verbo escritos.", null, null, null)],
                        [new(101, "O aluno"), new(103, "chegaram")]),
                    RespostasInternas = new Dictionary<int, int> { [101] = 1, [103] = 2 }
                }, ct);

    private static HttpResponseMessage Resposta(int status, string texto) =>
        new((HttpStatusCode)status) { Content = new StringContent(texto) };

    private sealed class CapturaLog : ILogger<LlmRouterDiagnostics>
    {
        public List<Dictionary<string, object?>> Eventos { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Eventos.Add(Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state)
                .ToDictionary(campo => campo.Key, campo => campo.Value));
    }

    private sealed class FixtureHttpFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Handler(enviar));
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> enviar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) => enviar(req, ct);
    }
}
