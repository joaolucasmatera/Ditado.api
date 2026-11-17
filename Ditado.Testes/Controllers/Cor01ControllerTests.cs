using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ditado.Aplicacao.DTOs;
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

public class Cor01ControllerTests(CustomWebApplicationFactory factory) : TesteIntegracaoBase(factory)
{
    [Fact]
    public async Task AlunoRecebeSomenteClassificacoesSelecionadasEPreservaNota()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioAsync(TipoUsuario.Aluno);
        var externo = await CriarUsuarioAsync(TipoUsuario.Aluno);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        await GarantirEscolaEVinculoAsync(aluno.Id, PapelEscolar.Aluno);
        var regras = await GarantirRegrasPublicasAsync(TipoErro.Acentuacao, TipoErro.MaiusculasPontuacao, TipoErro.OmissaoLetra);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var turma = new Turma { EscolaId = escolaId, ProfessorResponsavelId = professor.Id, Nome = "Turma COR-01", Serie = 3, AnoLetivo = 2026 };
        turma.Alunos.Add(await db.Usuarios.SingleAsync(u => u.Id == aluno.Id));
        db.Turmas.Add(turma);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var semExecutor = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Omissão", descricao = "Definição para revisão", nivel = 1 }));
        Assert.False(semExecutor.TemRegraExecutavel);
        var criacao = await PostAsyncRaw("/api/Ditados", Pedido([regras[0], regras[2], semExecutor.Id]));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var criado = Assert.IsType<DitadoResponse>(await criacao.Content.ReadFromJsonAsync<DitadoResponse>());
        var url = $"/api/Ditados/{criado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        var detalhes = Assert.IsType<DitadoCompletoDto>(await GetAsync<DitadoCompletoDto>($"{url}/visualizar"));
        Assert.False(detalhes.PossuiRespostas);
        Assert.Equal(3, detalhes.ErrosCatalogo.Count);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados",
            new { ditadoId = criado.Id, dataLimite = DateTime.UtcNow.AddDays(3) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await Client.PutAsJsonAsync(url, new { erroCatalogoIds = new[] { regras[0], regras[1], semExecutor.Id } })).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var realizacao = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
        var lacunas = realizacao.Segmentos.Where(s => s.SegmentoId.HasValue).Select(s => s.SegmentoId!.Value).ToArray();
        var pedido = new SubmeterRespostaRequest
        {
            Respostas =
            [
                new() { SegmentoId = lacunas[0], Resposta = "Cafe" },
                new() { SegmentoId = lacunas[1], Resposta = "pato" },
                new() { SegmentoId = lacunas[2], Resposta = " gato " }
            ]
        };
        AdicionarTokenAutorizacao(externo.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsyncRaw($"{url}/submeter", pedido)).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var resultado = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter", pedido));
        Assert.Equal(1, resultado.Acertos);
        Assert.Equal(1m / 3m * 100m, resultado.Nota);
        Assert.Equal("café", resultado.Detalhes[0].RespostaEsperada);
        Assert.Equal("Cafe", resultado.Detalhes[0].RespostaFornecida);
        Assert.Equal(2, resultado.Detalhes[0].TiposErro.Count);
        Assert.Contains(resultado.Detalhes[0].Classificacoes, c => c.ErroCatalogoId == regras[0] && c.Nome == "Regra Acentuacao" && c.TipoErro == 2);
        Assert.Contains(resultado.Detalhes[0].Classificacoes, c => c.ErroCatalogoId == regras[1] && c.TipoErro == 28);
        Assert.Empty(resultado.Detalhes[1].Classificacoes);
        Assert.Empty(resultado.Detalhes[1].TiposErro);
        Assert.Equal(nameof(EstadoClassificacao.NaoClassificado), resultado.Detalhes[1].EstadoClassificacao);
        Assert.True(resultado.Detalhes[2].Correto);

        var tentativa = await db.RespostaDitados.AsNoTracking().Include(r => r.RespostasSegmentos)
            .ThenInclude(r => r.TiposErro).SingleAsync(r => r.DitadoId == criado.Id);
        Assert.Equal(escolaId, tentativa.EscolaId);
        Assert.NotNull(tentativa.AplicacaoDitadoId);
        Assert.Equal(regras.Take(2).Order(), tentativa.RespostasSegmentos.SelectMany(s => s.TiposErro)
            .Select(t => t.ErroCatalogoId!.Value).Order());

        AdicionarTokenAutorizacao(professor.Token);
        var bloqueado = await Client.PutAsJsonAsync(url, new { erroCatalogoIds = new[] { regras[2] } });
        Assert.Equal(HttpStatusCode.BadRequest, bloqueado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync(url, new { titulo = "Título atualizado" })).StatusCode);
        detalhes = Assert.IsType<DitadoCompletoDto>(await GetAsync<DitadoCompletoDto>($"{url}/visualizar"));
        Assert.True(detalhes.PossuiRespostas);
        Assert.Equal(new[] { regras[0], regras[1], semExecutor.Id }.Order(), detalhes.ErrosCatalogo.Select(e => e.Id).Order());

        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync($"/api/ErrosCatalogo/{regras[2]}",
            new { nome = "Renomear regra", descricao = "A associação anterior congela a definição" })).StatusCode);
    }

    [Fact]
    public async Task CriacaoRecusaSelecaoVaziaInexistenteArquivadaOuDeOutraEscola()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        await GarantirEscolaEVinculoAsync(professor.Id);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var escolaExterna = new Escola { Codigo = "ESC-EXTERNA", Nome = "Escola externa" };
        var externo = new ErroCatalogo { Codigo = "ERRO-EXT", Nome = "Externo", AutorId = professor.Id };
        externo.Publicacoes.Add(new PublicacaoErroCatalogo
        {
            Nivel = NivelVisibilidade.Instituicao, Escola = escolaExterna, Versao = 1, ChaveVigente = 1, CriadaPorId = professor.Id
        });
        var arquivado = new ErroCatalogo { Codigo = "ERRO-ARQ", Nome = "Arquivado", AutorId = professor.Id, Ativo = false };
        arquivado.Publicacoes.Add(new PublicacaoErroCatalogo { Nivel = NivelVisibilidade.PublicoGeral, Versao = 1, ChaveVigente = 1, CriadaPorId = professor.Id });
        db.AddRange(externo, arquivado);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var disponivel = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.Acentuacao));
        var antes = await db.Ditados.CountAsync();
        foreach (var ids in new int[][] { [], [disponivel, int.MaxValue], [externo.Id], [arquivado.Id] })
            Assert.Equal(HttpStatusCode.BadRequest, (await PostAsyncRaw("/api/Ditados", Pedido(ids))).StatusCode);
        Assert.Equal(antes, await db.Ditados.CountAsync());
    }

    [Fact]
    public async Task AtribuicaoRespeitaAsTurmasAutorizadasPelaSelecao()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var destino = new Turma { Nome = "Turma autorizada", EscolaId = escolaId, ProfessorResponsavelId = professor.Id };
        var outraTurma = new Turma { Nome = "Outra turma", EscolaId = escolaId, ProfessorResponsavelId = professor.Id };
        db.Turmas.AddRange(destino, outraTurma);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var erro = Assert.IsType<ErroPublicadoResponse>(await PostAsync<ErroPublicadoResponse>("/api/ErrosCatalogo",
            new { nome = "Escopo por turma", descricao = "Definição restrita", nivel = 3, turmaIds = new[] { destino.Id } }));
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", Pedido([erro.Id])));
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Ditados/{ditado.Id}/visibilidade", new { nivel = 1 })).StatusCode);

        var pedido = new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(3) };
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsyncRaw($"/api/Turmas/{outraTurma.Id}/ditados", pedido)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{destino.Id}/ditados", pedido)).StatusCode);
        var aplicacao = await db.AplicacoesDitados.AsNoTracking().Include(a => a.ErrosSelecionados).SingleAsync(a => a.DitadoId == ditado.Id);
        Assert.Equal(destino.Id, aplicacao.TurmaId);
        Assert.Equal(erro.Id, Assert.Single(aplicacao.ErrosSelecionados).ErroCatalogoId);
    }

    [Fact]
    public async Task DitadoLegadoSemSelecaoPreservaClassificacaoSemVinculoDeCatalogo()
    {
        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var autorId = await db.Usuarios.Where(u => u.AdministradorGeral).Select(u => u.Id).FirstAsync();
        var lacuna = new DitadoSegmento { Tipo = TipoSegmento.Lacuna, Conteudo = "café" };
        var ditado = new Dominio.Entidades.Ditado { Titulo = "Legado", AutorId = autorId, Segmentos = [lacuna] };
        db.Ditados.Add(ditado);
        await db.SaveChangesAsync();

        var resultado = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"/api/Ditados/{ditado.Id}/submeter",
            new SubmeterRespostaRequest { Respostas = [new() { SegmentoId = lacuna.Id, Resposta = "cafe" }] }));
        var detalhe = Assert.Single(resultado.Detalhes);
        Assert.Single(detalhe.TiposErro);
        Assert.Empty(detalhe.Classificacoes);
        var tipo = await db.RespostaSegmentoTiposErro.SingleAsync(t => t.RespostaSegmento.RespostaDitadoId == resultado.RespostaDitadoId);
        Assert.Equal(TipoErro.Acentuacao, tipo.TipoErro);
        Assert.Null(tipo.ErroCatalogoId);
    }

    [Fact]
    public async Task SwaggerExplicitaSelecaoEIdentidadeDaClassificacao()
    {
        var swagger = await Client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var schemas = swagger.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("CriarDitadoRequest").GetProperty("properties").TryGetProperty("erroCatalogoIds", out _));
        Assert.True(schemas.GetProperty("DitadoCompletoDto").GetProperty("properties").TryGetProperty("possuiRespostas", out _));
        Assert.True(schemas.GetProperty("DetalheRespostaDto").GetProperty("properties").TryGetProperty("classificacoes", out _));
    }

    private static object Pedido(int[] ids) => new
    {
        titulo = "Ditado COR-01", textoComMarcacoes = "[café] [prato] [gato]",
        audioBase64 = "data:audio/mpeg;base64,SUQzAwAAAAAAJlRQRTEAAAAcAAAAU291bmRKYXk=",
        erroCatalogoIds = ids
    };

    private async Task<(int Id, string Token)> CriarUsuarioAsync(TipoUsuario tipo)
    {
        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        var login = $"cor01-{Guid.NewGuid():N}@teste.local";
        var usuario = Assert.IsType<UsuarioResponse>(await PostAsync<UsuarioResponse>("/api/usuarios",
            new CriarUsuarioRequest { Nome = "Usuário de teste", Login = login, Senha = "senha123", Tipo = tipo }));
        var auth = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/usuarios/login", new LoginRequest { Login = login, Senha = "senha123" }));
        return (usuario.Id, auth.Token);
    }
}
