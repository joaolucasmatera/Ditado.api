using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Ditado.Testes.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ditado.Testes.Controllers;

public class MarketplaceControllerTests : TesteIntegracaoBase
{
    private static readonly CategoriaPedagogicaRequest CategoriaOrtografica = new() { Codigo = "ORTOGRAFIA_CONTEXTUAL" };
    private readonly CustomWebApplicationFactory factory;

    public MarketplaceControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData(NivelVisibilidade.PublicoGeral)]
    [InlineData(NivelVisibilidade.Instituicao)]
    public async Task Publicar_GravaVisibilidadeInicialEItemFicaDisponivelNoEscopo(NivelVisibilidade nivel)
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(autorId);
        var response = await PostAsyncRaw("/api/ErrosCatalogo", new
        {
            nome = "Confusão de grafia",
            categoriaPedagogica = CategoriaOrtografica,
            descricao = "Descrição da ocorrência",
            nivel,
            turmaIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criado = Assert.IsType<ErroPublicadoResponse>(await response.Content.ReadFromJsonAsync<ErroPublicadoResponse>());
        Assert.Equal(nivel.ToString(), criado.NivelVisibilidade);
        Assert.Equal($"/api/ErrosCatalogo/{criado.Id}", response.Headers.Location!.AbsolutePath);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var publicacao = await db.PublicacoesErrosCatalogo.AsNoTracking().Include(p => p.TurmasAlvo)
            .SingleAsync(p => p.ErroCatalogoId == criado.Id);
        Assert.Equal(nivel, publicacao.Nivel);
        Assert.Equal(nivel == NivelVisibilidade.Instituicao ? escolaId : null, publicacao.EscolaId);
        Assert.Equal(1, publicacao.Versao);
        Assert.Equal(1, publicacao.ChaveVigente);
        Assert.Equal(autorId, publicacao.CriadaPorId);
        Assert.Empty(publicacao.TurmasAlvo);

        var colegaId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(colegaId);
        var ativos = await Client.GetFromJsonAsync<List<ErroPublicadoResponse>>("/api/ErrosCatalogo?estado=ativo");
        Assert.Equivalent(criado, Assert.Single(ativos!, erro => erro.Id == criado.Id), strict: true);
        Assert.Equivalent(criado, await Client.GetFromJsonAsync<ErroPublicadoResponse>(response.Headers.Location), strict: true);

        var professorOutraEscolaId = await AutenticarAsync(TipoUsuario.Professor);
        var outraEscola = new Escola { Nome = "Outra escola", Codigo = Guid.NewGuid().ToString("N") };
        db.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professorOutraEscolaId, Escola = outraEscola, Papel = PapelEscolar.Professor });
        await db.SaveChangesAsync();

        ativos = await Client.GetFromJsonAsync<List<ErroPublicadoResponse>>("/api/ErrosCatalogo?estado=ativo");
        if (nivel == NivelVisibilidade.PublicoGeral)
        {
            Assert.Equivalent(criado, Assert.Single(ativos!, erro => erro.Id == criado.Id), strict: true);
            Assert.Equivalent(criado, await Client.GetFromJsonAsync<ErroPublicadoResponse>(response.Headers.Location), strict: true);
        }
        else
        {
            Assert.DoesNotContain(ativos!, erro => erro.Id == criado.Id);
            Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync(response.Headers.Location)).StatusCode);
        }
    }

    [Theory]
    [InlineData(NivelVisibilidade.PublicoGeral, true, false)]
    [InlineData(NivelVisibilidade.PublicoGeral, false, true)]
    [InlineData(NivelVisibilidade.Instituicao, false, true)]
    [InlineData(NivelVisibilidade.Turmas, false, false)]
    [InlineData(NivelVisibilidade.Turmas, false, true)]
    [InlineData((NivelVisibilidade)99, false, false)]
    public async Task Publicar_EscopoInvalidoRetorna400SemGravar(NivelVisibilidade nivel, bool informarEscola, bool informarTurma)
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        var escola = await GarantirEscolaEVinculoAsync(autorId);
        await AssertRecusadoSemGravar(new
        {
            nome = "Inválido",
            categoriaPedagogica = CategoriaOrtografica,
            descricao = "Escopo inválido",
            nivel,
            escolaId = informarEscola ? escola : (int?)null,
            turmaIds = informarTurma ? new[] { int.MaxValue } : Array.Empty<int>()
        });
    }

    [Fact]
    public async Task Publicar_ProfessorSemVinculoRetorna403SemGravar()
    {
        await AutenticarAsync(TipoUsuario.Professor);
        var antes = await ContarRegistrosAsync();
        var response = await PostAsyncRaw("/api/ErrosCatalogo", new
        {
            nome = "Sem vínculo",
            descricao = "Sem vínculo escolar ativo",
            nivel = NivelVisibilidade.PublicoGeral
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(antes, await ContarRegistrosAsync());
    }

    [Theory]
    [InlineData(TipoUsuario.Professor, false)]
    [InlineData(TipoUsuario.Professor, true)]
    [InlineData(TipoUsuario.Administrador, false)]
    public async Task AlterarVisibilidade_ForaDaAutoriaOuEscolaPreservaPublicacao(TipoUsuario tipo, bool mesmaEscola)
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(autorId);
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Uso institucional", descricao = "Conteúdo da escola", categoriaPedagogica = CategoriaOrtografica }));

        var atorId = await AutenticarAsync(tipo, administradorGeral: false);
        var papel = tipo == TipoUsuario.Professor ? PapelEscolar.Professor : PapelEscolar.Administrador;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        if (mesmaEscola)
        {
            await GarantirEscolaEVinculoAsync(atorId, papel);
        }
        else
        {
            var escola = new Escola { Nome = "Escola externa", Codigo = Guid.NewGuid().ToString("N") };
            db.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = atorId, Escola = escola, Papel = papel });
            await db.SaveChangesAsync();
        }

        var url = $"/api/ErrosCatalogo/{criado.Id}";
        var leituraEsperada = mesmaEscola ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        Assert.Equal(leituraEsperada, (await GetAsyncRaw(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsyncRaw("/api/ErrosCatalogo/2147483647/visibilidade", new { nivel = 1 })).StatusCode);
        Assert.Equal(leituraEsperada, (await GetAsyncRaw(url)).StatusCode);

        var vigente = await db.PublicacoesErrosCatalogo.AsNoTracking().SingleAsync(p => p.ErroCatalogoId == criado.Id);
        Assert.Equal(NivelVisibilidade.Instituicao, vigente.Nivel);
        Assert.Equal(escolaId, vigente.EscolaId);
        Assert.Equal(1, vigente.Versao);
        Assert.Equal(1, vigente.ChaveVigente);
        Assert.Null(vigente.EncerradaEm);
    }

    [Theory]
    [InlineData(TipoUsuario.Professor, false)]
    [InlineData(TipoUsuario.Administrador, false)]
    [InlineData(TipoUsuario.Administrador, true)]
    public async Task AlterarVisibilidade_AutorOuAdminAutorizadoVersionaPublicacao(TipoUsuario tipo, bool administradorGeral)
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Uso institucional", descricao = "Conteúdo da escola", categoriaPedagogica = CategoriaOrtografica }));

        var atorId = autorId;
        if (tipo == TipoUsuario.Administrador)
        {
            atorId = await AutenticarAsync(tipo, administradorGeral);
            if (!administradorGeral)
                await GarantirEscolaEVinculoAsync(atorId, PapelEscolar.Administrador);
        }

        var url = $"/api/ErrosCatalogo/{criado.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        var recibo = Assert.IsType<ErroPublicadoResponse>(await Client.GetFromJsonAsync<ErroPublicadoResponse>(url));
        Assert.Equal(nameof(NivelVisibilidade.PublicoGeral), recibo.NivelVisibilidade);
        Assert.Equal(autorId, recibo.AutorId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var publicacoes = await db.PublicacoesErrosCatalogo.AsNoTracking()
            .Where(p => p.ErroCatalogoId == criado.Id).OrderBy(p => p.Versao).ToListAsync();
        Assert.Equal(2, publicacoes.Count);
        Assert.Null(publicacoes[0].ChaveVigente);
        Assert.NotNull(publicacoes[0].EncerradaEm);
        Assert.Equal(atorId, publicacoes[0].EncerradaPorId);
        Assert.Equal(2, publicacoes[1].Versao);
        Assert.Equal(1, publicacoes[1].ChaveVigente);
        Assert.Null(publicacoes[1].EscolaId);
    }

    [Fact]
    public async Task AlterarVisibilidade_AutorNaoRepublicaParaEscolaSemVinculo()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Uso institucional", descricao = "Conteúdo da escola", categoriaPedagogica = CategoriaOrtografica }));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var destino = new Escola { Nome = "Outro destino", Codigo = Guid.NewGuid().ToString("N") };
        db.Escolas.Add(destino);
        await db.SaveChangesAsync();

        var resposta = await PostAsyncRaw($"/api/ErrosCatalogo/{criado.Id}/visibilidade", new { nivel = 2, escolaId = destino.Id });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var vigente = await db.PublicacoesErrosCatalogo.AsNoTracking().SingleAsync(p => p.ErroCatalogoId == criado.Id);
        Assert.Equal(1, vigente.ChaveVigente);
        Assert.NotEqual(destino.Id, vigente.EscolaId);
    }

    [Fact]
    public async Task Exemplos_PublicaReleEditaERemoveSemPerderOutroPar()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var segundo = new ExemploErroDto("tambor", "tanbor");
        const string nome = "Troca de M por N";
        const string descricao = "Troca de M por N antes de P ou B";
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome, descricao, categoriaPedagogica = CategoriaOrtografica, exemplos = new[] { new ExemploErroDto(" campo ", " canpo "), segundo } }));
        var url = $"/api/ErrosCatalogo/{criado.Id}";
        var esperado = new[] { new ExemploErroDto("campo", "canpo"), segundo };
        Assert.Equal(esperado, criado.Exemplos);
        Assert.Equal(esperado, (await GetAsync<ErroPublicadoResponse>(url))!.Exemplos);

        var editado = new ExemploErroDto("tempo", "tenpo");
        var atualizacao = await PutAsync<ErroPublicadoResponse>(url,
            new { nome, descricao, exemplos = new[] { editado, segundo } });
        Assert.Equal(new[] { editado, segundo }, atualizacao!.Exemplos);

        // Um cliente antigo ainda pode editar o primeiro exemplo de uma definição com vários pares.
        atualizacao = await PutAsync<ErroPublicadoResponse>(url,
            new { nome, descricao, exemploEsperado = "sombrio", exemploResposta = "sonbrio" });
        Assert.Equal(new[] { new ExemploErroDto("sombrio", "sonbrio"), segundo }, atualizacao!.Exemplos);

        await PutAsync<ErroPublicadoResponse>(url, new { nome, descricao, exemplos = new[] { segundo } });
        var relido = (await GetAsync<ErroPublicadoResponse>(url))!;
        Assert.Equal(segundo, Assert.Single(relido.Exemplos));
        Assert.Equal((segundo.Esperado, segundo.Resposta), (relido.ExemploEsperado, relido.ExemploResposta));

        await PutAsync<ErroPublicadoResponse>(url, new { nome, descricao, exemplos = Array.Empty<ExemploErroDto>() });
        relido = (await GetAsync<ErroPublicadoResponse>(url))!;
        Assert.Empty(relido.Exemplos);
        Assert.Null(relido.ExemploEsperado);
        Assert.Null(relido.ExemploResposta);
    }

    [Fact]
    public async Task Exemplos_LeParDeCargaLegadaERejeitaListaIncompleta()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        using var legado = JsonDocument.Parse("""
            {"nome":"Troca antes de P","descricao":"Exemplo de carga legada","categoriaPedagogica":{"codigo":"ORTOGRAFIA_CONTEXTUAL"},"exemploEsperado":"campo","exemploResposta":"canpo"}
            """);
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", legado.RootElement));
        var relido = (await GetAsync<ErroPublicadoResponse>($"/api/ErrosCatalogo/{criado.Id}"))!;
        Assert.Equal(new ExemploErroDto("campo", "canpo"), Assert.Single(relido.Exemplos));

        var rejeitado = await PostAsyncRaw("/api/ErrosCatalogo", new
        {
            nome = "Definição inválida", descricao = "Par incompleto", categoriaPedagogica = CategoriaOrtografica,
            exemplos = new[] { new { esperado = "campo", resposta = " " } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejeitado.StatusCode);
        rejeitado = await PostAsyncRaw("/api/ErrosCatalogo", new
        {
            nome = "Definição inválida", descricao = "Par nulo", categoriaPedagogica = CategoriaOrtografica,
            exemplos = new object?[] { null }
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejeitado.StatusCode);
    }

    [Fact]
    public async Task Exemplos_AceitaCincoERejeitaSeisNaPublicacaoENaEdicao()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        const string nome = "Definição com exemplos";
        const string descricao = "Teste do limite";
        var seis = Enumerable.Range(1, 6).Select(i => new ExemploErroDto($"esperado {i}", $"resposta {i}")).ToArray();
        var cinco = seis[..5];
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome, descricao, categoriaPedagogica = CategoriaOrtografica, exemplos = cinco }));
        Assert.Equal(cinco, criado.Exemplos);
        var excesso = new { nome, descricao, categoriaPedagogica = CategoriaOrtografica, exemplos = seis };
        var post = await PostAsyncRaw("/api/ErrosCatalogo", excesso);
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        var url = $"/api/ErrosCatalogo/{criado.Id}";
        var put = await Client.PutAsJsonAsync(url, excesso);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.Contains("Informe até 5 exemplos.", (await put.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors").GetProperty("Exemplos")[0].GetString());
        Assert.Equal(cinco, (await GetAsync<ErroPublicadoResponse>(url))!.Exemplos);
    }

    [Fact]
    public async Task CategoriaPedagogica_PublicaGealeEEditaParaCategoriaNaoOrtografica()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var opcoes = (await GetAsync<List<CategoriaPedagogicaDto>>("/api/ErrosCatalogo/categorias-pedagogicas"))!;
        Assert.Equal(7, opcoes.Count);
        Assert.Equal(3, opcoes.Select(c => c.Familia).OfType<string>().Distinct().Count());
        var categoriaGeale = Assert.Single(opcoes, c => c.Codigo == "HIBRIDO_ACENTUACAO");
        Assert.Equal(("Acentuação", "Híbrida"), (categoriaGeale.Nome, categoriaGeale.Familia));
        var criado = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Acento gráfico", descricao = "Definição de teste", categoriaPedagogica = new { categoriaGeale.Codigo } }));
        var url = $"/api/ErrosCatalogo/{criado.Id}";
        Assert.Equal(categoriaGeale, criado.CategoriaPedagogica);
        Assert.Equal(categoriaGeale, (await GetAsync<ErroPublicadoResponse>(url))!.CategoriaPedagogica);

        var atualizado = await PutAsync<ErroPublicadoResponse>(url, new
        {
            nome = "Concordância", descricao = "Definição de teste",
            categoriaPedagogica = new { codigo = "NAO_ORTOGRAFICO", nome = " Concordância verbal " }
        });
        var naoOrtografica = new CategoriaPedagogicaDto("NAO_ORTOGRAFICO", "Concordância verbal", null);
        Assert.Equal(naoOrtografica, atualizado!.CategoriaPedagogica);
        var lista = (await GetAsync<List<ErroPublicadoResponse>>("/api/ErrosCatalogo"))!;
        Assert.Equal(naoOrtografica, Assert.Single(lista, e => e.Id == criado.Id).CategoriaPedagogica);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("INEXISTENTE", null)]
    [InlineData("NAO_ORTOGRAFICO", " ")]
    [InlineData("ORTOGRAFIA_CONTEXTUAL", "Rótulo diferente")]
    public async Task CategoriaPedagogica_RejeitaCategoriaAusenteOuInvalida(string? codigo, string? nomeCategoria)
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var categoriaPedagogica = codigo is null ? null : new { codigo, nome = nomeCategoria };
        await AssertRecusadoSemGravar(new { nome = "Erro inválido", descricao = "Definição de teste", categoriaPedagogica });
    }

    [Fact]
    public async Task CategoriaPedagogica_PreservaRegistroLegadoSemInferirPeloNome()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var legado = new ErroCatalogo { Codigo = "PER70-LEGADO", Nome = "Acentuação legada", AutorId = autorId };
        db.ErrosCatalogo.Add(legado);
        await db.SaveChangesAsync();
        var url = $"/api/ErrosCatalogo/{legado.Id}";
        Assert.Null((await GetAsync<ErroPublicadoResponse>(url))!.CategoriaPedagogica);
        var atualizado = await PutAsync<ErroPublicadoResponse>(url, new { nome = legado.Nome, descricao = "Descrição revisada" });
        Assert.Null(atualizado!.CategoriaPedagogica);
    }

    [Fact]
    public async Task Swagger_DocumentaPublicacaoEAlteracaoDeVisibilidade()
    {
        var documento = await Client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var post = documento.GetProperty("paths").GetProperty("/api/ErrosCatalogo").GetProperty("post");
        Assert.Equal("Publica um novo erro no catálogo.", post.GetProperty("summary").GetString());
        var respostas = post.GetProperty("responses");
        foreach (var status in new[] { "201", "400", "401", "403" })
            Assert.True(respostas.TryGetProperty(status, out _), $"Resposta {status} ausente no Swagger.");
        var corpo = post.GetProperty("requestBody").GetProperty("content").GetProperty("application/json");
        Assert.Equal("#/components/schemas/PublicarErroRequest", corpo.GetProperty("schema").GetProperty("$ref").GetString());
        Assert.Contains(documento.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("Bearer", out _));
        var esquemas = documento.GetProperty("components").GetProperty("schemas");
        var categorias = documento.GetProperty("paths").GetProperty("/api/ErrosCatalogo/categorias-pedagogicas").GetProperty("get");
        Assert.True(categorias.GetProperty("responses").TryGetProperty("403", out _));
        foreach (var esquema in new[] { "PublicarErroRequest", "ErroPublicadoResponse" })
        {
            Assert.True(esquemas.GetProperty(esquema).GetProperty("properties").TryGetProperty("categoriaPedagogica", out _));
            var exemplos = esquemas.GetProperty(esquema).GetProperty("properties").GetProperty("exemplos");
            Assert.Equal("array", exemplos.GetProperty("type").GetString());
            Assert.Equal("#/components/schemas/ExemploErroDto", exemplos.GetProperty("items").GetProperty("$ref").GetString());
            if (esquema == "PublicarErroRequest")
                Assert.Equal(5, exemplos.GetProperty("maxItems").GetInt32());
        }

        var alterar = documento.GetProperty("paths").GetProperty("/api/ErrosCatalogo/{id}/visibilidade").GetProperty("post");
        Assert.Equal("Altera a visibilidade vigente de um erro do catálogo.", alterar.GetProperty("summary").GetString());
        var respostasAlterar = alterar.GetProperty("responses");
        foreach (var status in new[] { "204", "400", "401", "403", "404" })
            Assert.True(respostasAlterar.TryGetProperty(status, out _), $"Resposta {status} ausente no Swagger.");
    }

    [Theory]
    [InlineData(TipoUsuario.Administrador)]
    [InlineData(TipoUsuario.Professor)]
    public async Task Publicar_StaffPersisteRegistroCompletoEReciboEstavel(TipoUsuario tipo)
    {
        var autorId = await AutenticarAsync(tipo);
        await GarantirEscolaEVinculoAsync(autorId, tipo == TipoUsuario.Professor ? PapelEscolar.Professor : PapelEscolar.Administrador);
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var response = await PostAsyncRaw("/api/ErrosCatalogo", new PublicarErroRequest
        {
            Nome = " Troca de M por N ", Descricao = " Descrição pedagógica ",
            CategoriaPedagogica = CategoriaOrtografica,
            ExemploEsperado = " também ", ExemploResposta = " tanbém ",
            ObservacaoPedagogica = " Retomar a regularidade. ", SerieAno = " 3º ano "
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criado = await response.Content.ReadFromJsonAsync<ErroPublicadoResponse>();
        Assert.NotNull(criado);
        Assert.True(criado.Id > 0);
        Assert.Equal(autorId, criado.AutorId);
        Assert.True(criado.Ativo);
        Assert.False(criado.TemRegraExecutavel);
        Assert.InRange(criado.DataCriacao, antes, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(criado.DataCriacao, criado.DataAlteracao);
        Assert.False(string.IsNullOrWhiteSpace(criado.Codigo));
        Assert.Equal("Troca de M por N", criado.Nome);
        Assert.Equal("Descrição pedagógica", criado.Descricao);
        Assert.Equal("também", criado.ExemploEsperado);
        Assert.Equal("tanbém", criado.ExemploResposta);
        Assert.Equal("Retomar a regularidade.", criado.ObservacaoPedagogica);
        Assert.Equal("3º ano", criado.SerieAno);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(nameof(NivelVisibilidade.Instituicao), criado.NivelVisibilidade);
        var recibo = await Client.GetFromJsonAsync<ErroPublicadoResponse>(response.Headers.Location);
        Assert.Equivalent(criado, recibo, strict: true);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var persistido = await db.ErrosCatalogo.AsNoTracking().SingleAsync(e => e.Id == criado.Id);
        Assert.Equal(criado.Codigo, persistido.Codigo);
        Assert.Equal(criado.AutorId, persistido.AutorId);
        Assert.Equal(criado.ExemploResposta, persistido.ExemploResposta);
    }

    [Theory]
    [InlineData(null, "Descrição")]
    [InlineData("", "Descrição")]
    [InlineData("  ", "Descrição")]
    [InlineData("Nome", null)]
    [InlineData("Nome", "")]
    [InlineData("Nome", "  ")]
    public async Task Publicar_ObrigatoriosInvalidosNaoGravam(string? nome, string? descricao)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        await AssertRecusadoSemGravar(new { nome, descricao, categoriaPedagogica = CategoriaOrtografica });
    }

    [Theory]
    [InlineData("também", null)]
    [InlineData(null, "tanbém")]
    [InlineData("também", " ")]
    [InlineData(" ", "tanbém")]
    public async Task Publicar_ExemploIncompletoNaoGrava(string? esperado, string? resposta)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        await AssertRecusadoSemGravar(new { nome = "Nome", descricao = "Descrição", categoriaPedagogica = CategoriaOrtografica,
            exemploEsperado = esperado, exemploResposta = resposta });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Publicar_OpcionaisVaziosSaoNulos(string? valor)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        var criado = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", new
        {
            nome = "Opcionais", descricao = "Sem opcionais", exemploEsperado = valor,
            categoriaPedagogica = CategoriaOrtografica,
            exemploResposta = valor, observacaoPedagogica = valor, serieAno = valor,
            nivel = NivelVisibilidade.PublicoGeral
        });
        Assert.NotNull(criado);
        Assert.Null(criado.ExemploEsperado);
        Assert.Null(criado.ExemploResposta);
        Assert.Null(criado.ObservacaoPedagogica);
        Assert.Null(criado.SerieAno);
    }

    [Fact]
    public async Task Publicar_NomeRepetidoPermitidoIdentidadesDistintasEOpcionaisOmitidos()
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        var request = new { nome = "Mesmo nome", descricao = "Nenhuma unicidade de nome",
            categoriaPedagogica = CategoriaOrtografica, nivel = NivelVisibilidade.PublicoGeral };
        var a = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", request);
        var b = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", request);
        Assert.NotNull(a); Assert.NotNull(b);
        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(a.Codigo, b.Codigo);
        Assert.Null(a.ExemploEsperado);
    }

    [Fact]
    public async Task Atualizar_AutorRecebe200EEndpointNaoRetorna405()
    {
        var autorId = await AutenticarAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(autorId);
        var criado = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Antes", descricao = "Descrição antes", categoriaPedagogica = CategoriaOrtografica });
        Assert.NotNull(criado);

        var response = await Client.PutAsJsonAsync($"/api/ErrosCatalogo/{criado.Id}", new { nome = "Depois", descricao = "Descrição depois" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atualizado = await response.Content.ReadFromJsonAsync<ErroPublicadoResponse>();
        Assert.Equal("Depois", atualizado!.Nome);
        Assert.Equal(criado.CategoriaPedagogica, atualizado.CategoriaPedagogica);
    }

    [Theory]
    [InlineData("id", 123)]
    [InlineData("autorId", 123)]
    [InlineData("codigo", "FORJADO")]
    [InlineData("ativo", false)]
    [InlineData("dataCriacao", "2000-01-01T00:00:00Z")]
    [InlineData("regraCodigo", "ACENTUACAO")]
    [InlineData("tipoErro", 2)]
    [InlineData("executor", "codigo")]
    [InlineData("prompt", "instruções")]
    [InlineData("modelo", "modelo")]
    public async Task Publicar_CamposNaoPermitidosSaoRejeitados(string campo, object valor)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        await AssertRecusadoSemGravar(new Dictionary<string, object>
            { ["nome"] = "Nome", ["descricao"] = "Descrição", ["categoriaPedagogica"] = CategoriaOrtografica, [campo] = valor });
    }

    [Fact]
    public async Task Publicar_AlunoEAnonimoNaoGravamNemLeemRecibo()
    {
        var body = new { nome = "Não autorizado", descricao = "Sem escrita" };
        var antes = await ContarRegistrosAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsyncRaw("/api/ErrosCatalogo", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsyncRaw("/api/ErrosCatalogo/1")).StatusCode);
        await AutenticarAsync(TipoUsuario.Aluno);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsyncRaw("/api/ErrosCatalogo", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsyncRaw("/api/ErrosCatalogo/1")).StatusCode);
        Assert.Equal(antes, await ContarRegistrosAsync());
    }

    [Fact]
    public async Task ReciboInexistenteRetorna404SemListagemAmpla()
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsyncRaw("/api/ErrosCatalogo/2147483647")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsyncRaw("/api/ErrosCatalogo")).StatusCode);
    }

    [Theory]
    [InlineData("nome", 201)]
    [InlineData("descricao", 10001)]
    [InlineData("exemploEsperado", 2001)]
    [InlineData("observacaoPedagogica", 10001)]
    [InlineData("serieAno", 201)]
    public async Task Publicar_LimitesTecnicosValidamAntesDaGravacao(string campo, int tamanho)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        var dados = new Dictionary<string, object>
            { ["nome"] = "Nome", ["descricao"] = "Descrição", ["categoriaPedagogica"] = CategoriaOrtografica, [campo] = new string('a', tamanho) };
        await AssertRecusadoSemGravar(dados);
    }

    private async Task AssertRecusadoSemGravar(object dados)
    {
        var antes = await ContarRegistrosAsync();
        var response = await PostAsyncRaw("/api/ErrosCatalogo", dados);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(antes, await ContarRegistrosAsync());
    }

    private async Task<(int Erros, int Publicacoes)> ContarRegistrosAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        return (await db.ErrosCatalogo.CountAsync(), await db.PublicacoesErrosCatalogo.CountAsync());
    }

    private async Task<int> AutenticarAsync(TipoUsuario tipo, bool administradorGeral = true)
    {
        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        if (tipo == TipoUsuario.Administrador && administradorGeral)
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<DitadoDbContext>().Usuarios
                .Where(u => u.Tipo == tipo && u.AdministradorGeral).Select(u => u.Id).FirstAsync();
        }
        var login = $"marketplace-{Guid.NewGuid():N}@teste.local";
        var usuario = await PostAsync<UsuarioResponse>("/api/usuarios", new CriarUsuarioRequest
        {
            Nome = "Usuário Marketplace", Login = login, Senha = "senha123", Tipo = tipo
        });
        Assert.NotNull(usuario);
        var auth = await PostAsync<LoginResponse>("/api/usuarios/login", new LoginRequest { Login = login, Senha = "senha123" });
        Assert.NotNull(auth);
        AdicionarTokenAutorizacao(auth.Token);
        return usuario.Id;
    }
}

