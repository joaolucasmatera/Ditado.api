using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.DTOs.Turmas;
using Ditado.Aplicacao.Llm;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Ditado.Testes.Infra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Ditado.Testes.Controllers;

public sealed class Per69ControllerTests(Per69Factory factory, ITestOutputHelper output) : TesteIntegracaoBase(factory), IClassFixture<Per69Factory>
{
    [Fact]
    public async Task CorrigeCincoErrosCadastradosEmUmLote()
    {
        factory.Router.Caso = "cinco";
        factory.Router.Captura = output.WriteLine;
        var professor = await CriarUsuarioSinteticoAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioSinteticoAsync(TipoUsuario.Aluno);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        await GarantirEscolaEVinculoAsync(aluno.Id, PapelEscolar.Aluno);
        var acentuacao = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.Acentuacao));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var turma = new Turma { EscolaId = escolaId, ProfessorResponsavelId = professor.Id, Nome = "Turma sintética de cinco erros" };
        turma.Alunos.Add(await db.Usuarios.SingleAsync(u => u.Id == aluno.Id));
        db.Turmas.Add(turma);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var definicoes = new (string Nome, string Descricao, string Esperado, string Escrita)[]
        {
            ("Concordância verbal", "Marque apenas a lacuna do verbo que discorda em número do sujeito escrito na mesma frase.", "Os alunos chegaram", "Os alunos chegou"),
            ("Troca inicial de ch por x", "Marque quando a única diferença for substituir ch inicial por x.", "chave", "xave"),
            ("Supressão de til em ão", "Marque quando a única diferença for substituir ão final por ao.", "portão", "portao"),
            ("Redução de ss para s", "Marque quando a única diferença for substituir ss entre vogais por s.", "passou", "pasou"),
            ("Troca de m por n antes de p", "Marque quando a única diferença for substituir m por n imediatamente antes de p.", "campo", "canpo")
        };
        var ids = new List<int>();
        foreach (var (nome, descricao, exemploEsperado, exemploResposta) in definicoes)
        {
            object request = ids.Count == 0
                ? new { nome, descricao, categoriaPedagogica = new { codigo = "NAO_ORTOGRAFICO", nome = "Concordância verbal" },
                    exemplos = new[] { new ExemploErroDto(exemploEsperado, exemploResposta),
                    new ExemploErroDto("Os pássaros voaram", "Os pássaros voou") }, nivel = 1 }
                : new { nome, descricao, exemploEsperado, exemploResposta, nivel = 1,
                    categoriaPedagogica = new { codigo = "ORTOGRAFIA_CONTEXTUAL" } };
            var publicado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", request));
            Assert.False(publicado.TemRegraExecutavel);
            ids.Add(publicado.Id);
        }
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", new
        {
            titulo = "Ditado sintético de cinco erros",
            textoComMarcacoes = "Os alunos [chegaram]. A [chave] abriu o [portão]. O trem [passou] perto do [campo]. O sino [tocou]. O [café] esfriou.",
            audioBase64 = "data:audio/mpeg;base64,SUQzAwAAAAAAJlRQRTEAAAAcAAAAU291bmRKYXk=",
            erroCatalogoIds = ids.Append(acentuacao).ToArray()
        }));
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados",
            new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(1) })).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var leitura = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
        var escritas = new[] { "chegou", "xave", "portao", "pasou", "canpo", "tocou", "cafe" };
        var respostas = leitura.Segmentos.Where(s => s.SegmentoId.HasValue)
            .Select((s, i) => new { segmentoId = s.SegmentoId, resposta = escritas[i] }).ToArray();
        var inicial = Assert.IsType<ResultadoDitadoResponse>(
            await PostAsync<ResultadoDitadoResponse>($"{url}/submeter", new { respostas }));
        Assert.All(inicial.Detalhes, d => Assert.Equal("Pendente", d.EstadoAnaliseLlm));
        var regra = Assert.Single(inicial.Detalhes[6].Classificacoes);
        Assert.Equal(("Codigo", acentuacao), (regra.Origem, regra.ErroCatalogoId));

        // Simula uma alteração na origem após o enfileiramento para testar a independência da cópia congelada.
        var catalogado = await db.ErrosCatalogo.SingleAsync(e => e.Id == ids[0]);
        catalogado.ExemploResposta = "Alteração posterior";
        catalogado.ExemplosAdicionaisJson = null;
        await db.SaveChangesAsync();

        var chamadasAntes = factory.Router.Chamadas;
        var processor = scope.ServiceProvider.GetRequiredService<LlmAnalysisProcessor>();
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.Equal(chamadasAntes + 1, factory.Router.Chamadas);
        var resultadoUrl = $"/api/Alunos/tentativas/{inicial.RespostaDitadoId}/resultado";
        var final = Assert.IsType<ResultadoDitadoResponse>(await GetAsync<ResultadoDitadoResponse>(resultadoUrl));
        Assert.Equal(14.29m, final.Nota);
        Assert.Equal(inicial.Nota, final.Nota);
        Assert.Equal(7, final.Detalhes.Count);
        Assert.All(final.Detalhes, d => Assert.Equal("Concluida", d.EstadoAnaliseLlm));
        Assert.All(final.Detalhes.Zip(inicial.Detalhes), par =>
            Assert.Equal(par.Second.Classificacoes, par.First.Classificacoes.Where(c => c.Origem == "Codigo")));
        var achados = final.Detalhes.Take(5).Select(d =>
            Assert.Single(d.Classificacoes.Where(c => c.Origem == "LLM"))).ToArray();
        Assert.Equal(ids, achados.Select(a => a.ErroCatalogoId));
        Assert.Empty(final.Detalhes[5].Classificacoes);
        Assert.Equal(regra, Assert.Single(final.Detalhes[6].Classificacoes));
        var relido = await GetAsync<ResultadoDitadoResponse>(resultadoUrl);
        Assert.Equal(JsonSerializer.Serialize(final), JsonSerializer.Serialize(relido));

        var trilha = await db.Set<LlmTentativaProvedor>().AsNoTracking()
            .SingleAsync(t => db.LlmAnalisesTarefas.Any(a => a.Id == t.LlmAnaliseTarefaId &&
                a.RespostaSegmento.RespostaDitadoId == inicial.RespostaDitadoId));
        Assert.False(string.IsNullOrWhiteSpace(trilha.Modelo));
        Assert.NotEqual(LlmOptions.RouterModel, trilha.Modelo);
        Assert.Equal(LlmOptions.RouterPromptVersion, trilha.PromptVersao);
        output.WriteLine(JsonSerializer.Serialize(new { routerReal = factory.Router.UsaRouterLocal, ids,
            final.Nota, final.Detalhes, trilha.Provedor, trilha.Modelo, trilha.PromptVersao, trilha.TentativasRouter }));
    }

    [Theory]
    [InlineData("valido")]
    [InlineData("indisponivel")]
    [InlineData("inconclusivo")]
    public async Task SubmissaoEReleituraPreservamCorrecaoHibrida(string caso)
    {
        factory.Router.Caso = caso;
        factory.Router.Captura = output.WriteLine;
        var professor = await CriarUsuarioSinteticoAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioSinteticoAsync(TipoUsuario.Aluno);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        await GarantirEscolaEVinculoAsync(aluno.Id, PapelEscolar.Aluno);
        var acentuacao = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.Acentuacao));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var turma = new Turma { EscolaId = escolaId, ProfessorResponsavelId = professor.Id, Nome = "Turma sintética PER-69" };
        turma.Alunos.Add(await db.Usuarios.SingleAsync(u => u.Id == aluno.Id));
        db.Turmas.Add(turma);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var docente = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", new
        {
            nome = "Concordância verbal sintética",
            descricao = "Marque somente o verbo que não concorda com o sujeito escrito na mesma frase.",
            categoriaPedagogica = new { codigo = "NAO_ORTOGRAFICO", nome = "Concordância verbal" },
            nivel = 1
        }));
        Assert.False(docente.TemRegraExecutavel);
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", new
        {
            titulo = "Ditado sintético PER-69",
            textoComMarcacoes = "O [café] esfriou. Os professores [chegaram] cedo. O sino [tocou].",
            audioBase64 = "data:audio/mpeg;base64,SUQzAwAAAAAAJlRQRTEAAAAcAAAAU291bmRKYXk=",
            erroCatalogoIds = new[] { acentuacao, docente.Id }
        }));
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados",
            new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(1) })).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var leitura = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
        var escritas = new[] { "cafe", "chegou", "tocou" };
        var respostas = leitura.Segmentos.Where(s => s.SegmentoId.HasValue)
            .Select((s, i) => new { segmentoId = s.SegmentoId, resposta = escritas[i] }).ToArray();
        var inicial = Assert.IsType<ResultadoDitadoResponse>(
            await PostAsync<ResultadoDitadoResponse>($"{url}/submeter", new { respostas }));
        Assert.Equal(33.33m, inicial.Nota);
        Assert.Equal("Codigo", Assert.Single(inicial.Detalhes[0].Classificacoes).Origem);
        Assert.All(inicial.Detalhes, d => Assert.Equal("Pendente", d.EstadoAnaliseLlm));

        var chamadasAntes = factory.Router.Chamadas;
        var processor = scope.ServiceProvider.GetRequiredService<LlmAnalysisProcessor>();
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.False(await processor.ProcessNextAsync(default));
        Assert.Equal(chamadasAntes + 1, factory.Router.Chamadas);
        var tentativaUrl = $"/api/Alunos/tentativas/{inicial.RespostaDitadoId}/resultado";
        var final = Assert.IsType<ResultadoDitadoResponse>(await GetAsync<ResultadoDitadoResponse>(tentativaUrl));
        var relido = Assert.IsType<ResultadoDitadoResponse>(await GetAsync<ResultadoDitadoResponse>(tentativaUrl));
        Assert.Equal(JsonSerializer.Serialize(final), JsonSerializer.Serialize(relido));
        Assert.Equal(inicial.Nota, final.Nota);
        Assert.Equal(inicial.Detalhes[0].Classificacoes, final.Detalhes[0].Classificacoes);
        Assert.Empty(final.Detalhes[2].Classificacoes);
        if (caso == "valido")
        {
            var achado = Assert.Single(final.Detalhes[1].Classificacoes);
            Assert.Equal(("LLM", docente.Id), (achado.Origem, achado.ErroCatalogoId));
            Assert.All(final.Detalhes, d => Assert.Equal("Concluida", d.EstadoAnaliseLlm));
        }
        else
        {
            Assert.Empty(final.Detalhes[1].Classificacoes);
            var estados = caso == "indisponivel" ? new[] { "Falha", "Falha", "Falha" } : ["Concluida", "Revisao", "Concluida"];
            Assert.Equal(estados, final.Detalhes.Select(d => d.EstadoAnaliseLlm));
        }

        var trilha = await db.Set<LlmTentativaProvedor>().AsNoTracking()
            .SingleAsync(t => db.LlmAnalisesTarefas.Any(a => a.Id == t.LlmAnaliseTarefaId &&
                a.RespostaSegmento.RespostaDitadoId == inicial.RespostaDitadoId));
        Assert.Equal(LlmOptions.RouterPromptVersion, trilha.PromptVersao);
        output.WriteLine(JsonSerializer.Serialize(new { caso, statusSubmissao = 200, statusReleitura = 200, inicial, final,
            trilha.Provedor, trilha.Modelo, trilha.TentativasRouter, trilha.PromptVersao, trilha.Desfecho, trilha.HttpStatus,
            trilha.Motivo, latenciaWorkerMs = (trilha.EncerradaEm - trilha.IniciadaEm)?.TotalMilliseconds }));
        if (caso == "indisponivel")
        {
            Assert.Equal("Rede", trilha.Motivo);
            Assert.Null(trilha.TentativasRouter);
        }
        else if (factory.Router.UsaRouterLocal)
        {
            Assert.NotEqual(LlmOptions.RouterProvider, trilha.Provedor);
            Assert.NotEqual(LlmOptions.RouterModel, trilha.Modelo);
            Assert.True(trilha.TentativasRouter > 0);
        }
        else
            Assert.Equal(("beta", "alternativo", 3), (trilha.Provedor, trilha.Modelo, trilha.TentativasRouter));
    }
}

public sealed class Per69Factory : CustomWebApplicationFactory
{
    internal RouterFixture Router { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<LlmOptions>(o =>
            {
                o.Enabled = true;
                o.RouterUrl = "http://127.0.0.1:4270/v1";
                o.TimeoutSeconds = 60;
                o.MaxOutputTokens = 4096;
            });
            var worker = services.Single(s => s.ServiceType == typeof(IHostedService) &&
                s.ImplementationType == typeof(LlmAnalysisWorker));
            services.Remove(worker);
            services.AddScoped<ILlmProviderClient>(sp =>
                new LlmProviderClient(Router, sp.GetRequiredService<IOptions<LlmOptions>>(), _ =>
                    Router.UsaRouterLocal ? Environment.GetEnvironmentVariable(LlmOptions.RouterCredential) : Guid.NewGuid().ToString("N")));
        });
    }

    internal sealed class RouterFixture : IHttpClientFactory
    {
        public string Caso { get; set; } = "";
        public int Chamadas { get; private set; }
        public Action<string>? Captura { get; set; }
        public bool UsaRouterLocal => Caso is "valido" or "cinco" &&
            Environment.GetEnvironmentVariable("DITADO_PER69_ROUTER_REAL") == "1";

        public HttpClient CreateClient(string name) => UsaRouterLocal
            ? new(new CapturaRouter(this))
            : new(new RespostaRouter(this));

        private sealed class CapturaRouter(RouterFixture router) : DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false })
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                router.Chamadas++;
                var inicio = System.Diagnostics.Stopwatch.StartNew();
                var payload = await request.Content!.ReadAsStringAsync(cancellationToken);
                using var entrada = JsonDocument.Parse(payload);
                var resposta = await base.SendAsync(request, cancellationToken);
                try
                {
                    using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(cancellationToken));
                    var result = corpo.RootElement;
                    router.Captura?.Invoke(JsonSerializer.Serialize(new
                    {
                        entrada = entrada.RootElement,
                        status = (int)resposta.StatusCode,
                        latenciaMs = inicio.ElapsedMilliseconds,
                        modelo = result.TryGetProperty("model", out var modelo) ? modelo.GetString() : null,
                        uso = result.TryGetProperty("usage", out var uso) ? uso : (JsonElement?)null,
                        fim = result.TryGetProperty("choices", out var escolhas) ? escolhas[0].GetProperty("finish_reason").GetString() : null,
                        saida = result.TryGetProperty("choices", out escolhas) ? escolhas[0].GetProperty("message").GetProperty("content").GetString() : null,
                    }));
                    return resposta;
                }
                catch
                {
                    resposta.Dispose();
                    throw;
                }
            }
        }

        private sealed class RespostaRouter(RouterFixture router) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                router.Chamadas++;
                Assert.Equal("http://127.0.0.1:4270/v1/chat/completions", request.RequestUri!.AbsoluteUri);
                if (router.Caso == "indisponivel") throw new HttpRequestException();
                using var envelope = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Assert.Equal("auto", envelope.RootElement.GetProperty("model").GetString());
                using var lote = JsonDocument.Parse(envelope.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
                var lacunas = lote.RootElement.GetProperty("respostas").EnumerateArray().ToArray();
                var candidatos = lote.RootElement.GetProperty("errosCandidatos").EnumerateArray().ToArray();
                if (router.Caso == "cinco")
                {
                    var multi = Assert.Single(candidatos, c => c.TryGetProperty("exemplos", out _));
                    Assert.False(multi.TryGetProperty("exemploResposta", out _));
                    var pares = multi.GetProperty("exemplos").Deserialize<string[][]>()!;
                    Assert.Equal(new[] { "Os alunos chegaram", "Os alunos chegou" }, pares[0]);
                    Assert.Equal(new[] { "Os pássaros voaram", "Os pássaros voou" }, pares[1]);
                    Assert.Equal(2, pares.Length);
                }
                var correspondencias = router.Caso == "cinco"
                    ? new[] { "Os alunos chegou", "xave", "portao", "pasou", "canpo" }.ToDictionary(
                        exemplo => exemplo.Split(' ')[^1],
                        exemplo => candidatos.Single(c => c.TryGetProperty("exemplos", out var pares)
                            ? pares[0][1].ValueEquals(exemplo)
                            : c.GetProperty("exemploResposta").ValueEquals(exemplo)).GetProperty("id").GetInt32())
                    : new Dictionary<string, int> { ["chegou"] = Assert.Single(candidatos).GetProperty("id").GetInt32() };
                var saida = JsonSerializer.Serialize(new
                {
                    segmentos = lacunas.Select(l => new
                    {
                        segmentoId = l.GetProperty("segmentoId").GetInt32(),
                        candidatosCorrespondentes = (router.Caso is "valido" or "cinco") &&
                            correspondencias.TryGetValue(l.GetProperty("resposta").GetString() ?? "", out var candidato)
                            ? new[] { candidato } : [],
                        inconclusivo = router.Caso == "inconclusivo" && l.GetProperty("resposta").ValueEquals("chegou")
                    })
                });
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        model = "beta/alternativo",
                        choices = new[] { new { finish_reason = "stop", message = new { content = saida } } }
                    })
                };
                response.Headers.Add("x-free-llm-provider", "beta");
                response.Headers.Add("x-free-llm-model", "alternativo");
                response.Headers.Add("x-free-llm-attempts", "3");
                return response;
            }
        }
    }
}
