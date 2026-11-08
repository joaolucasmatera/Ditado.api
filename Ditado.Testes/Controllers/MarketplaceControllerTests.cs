using System.Net;
using System.Net.Http.Json;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Ditado.Testes.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ditado.Testes.Controllers;

public class MarketplaceControllerTests : TesteIntegracaoBase
{
    private readonly CustomWebApplicationFactory factory;

    public MarketplaceControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData(TipoUsuario.Administrador)]
    [InlineData(TipoUsuario.Professor)]
    public async Task Publicar_StaffPersisteRegistroCompletoEReciboEstavel(TipoUsuario tipo)
    {
        var autorId = await AutenticarAsync(tipo);
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var response = await PostAsyncRaw("/api/ErrosCatalogo", new PublicarErroRequest
        {
            Nome = " Troca de M por N ", Descricao = " Descrição pedagógica ",
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
        if (tipo == TipoUsuario.Professor)
            await GarantirEscolaEVinculoAsync(autorId);
        else
            await GarantirEscolaEVinculoAsync(autorId, PapelEscolar.Administrador);
        var publicar = await PostAsyncRaw($"/api/ErrosCatalogo/{criado.Id}/visibilidade", new { nivel = NivelVisibilidade.PublicoGeral });
        Assert.Equal(HttpStatusCode.NoContent, publicar.StatusCode);
        var recibo = await Client.GetFromJsonAsync<ErroPublicadoResponse>(response.Headers.Location);
        Assert.Equal(criado, recibo);
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
        await AssertRecusadoSemGravar(new { nome, descricao });
    }

    [Theory]
    [InlineData("também", null)]
    [InlineData(null, "tanbém")]
    [InlineData("também", " ")]
    [InlineData(" ", "tanbém")]
    public async Task Publicar_ExemploIncompletoNaoGrava(string? esperado, string? resposta)
    {
        await AutenticarAsync(TipoUsuario.Administrador);
        await AssertRecusadoSemGravar(new { nome = "Nome", descricao = "Descrição", exemploEsperado = esperado, exemploResposta = resposta });
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
            exemploResposta = valor, observacaoPedagogica = valor, serieAno = valor
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
        var request = new { nome = "Mesmo nome", descricao = "Nenhuma unicidade de nome" };
        var a = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", request);
        var b = await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo", request);
        Assert.NotNull(a); Assert.NotNull(b);
        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(a.Codigo, b.Codigo);
        Assert.Null(a.ExemploEsperado);
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
        await AssertRecusadoSemGravar(new Dictionary<string, object> { ["nome"] = "Nome", ["descricao"] = "Descrição", [campo] = valor });
    }

    [Fact]
    public async Task Publicar_AlunoEAnonimoNaoGravamNemLeemRecibo()
    {
        var body = new { nome = "Não autorizado", descricao = "Sem escrita" };
        var antes = await ContarAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsyncRaw("/api/ErrosCatalogo", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsyncRaw("/api/ErrosCatalogo/1")).StatusCode);
        await AutenticarAsync(TipoUsuario.Aluno);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsyncRaw("/api/ErrosCatalogo", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsyncRaw("/api/ErrosCatalogo/1")).StatusCode);
        Assert.Equal(antes, await ContarAsync());
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
        var dados = new Dictionary<string, object> { ["nome"] = "Nome", ["descricao"] = "Descrição", [campo] = new string('a', tamanho) };
        await AssertRecusadoSemGravar(dados);
    }

    private async Task AssertRecusadoSemGravar(object dados)
    {
        var antes = await ContarAsync();
        var response = await PostAsyncRaw("/api/ErrosCatalogo", dados);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(antes, await ContarAsync());
    }

    private async Task<int> ContarAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DitadoDbContext>().ErrosCatalogo.CountAsync();
    }

    private async Task<int> AutenticarAsync(TipoUsuario tipo)
    {
        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        if (tipo == TipoUsuario.Administrador)
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<DitadoDbContext>().Usuarios.Where(u => u.Tipo == tipo).Select(u => u.Id).FirstAsync();
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

