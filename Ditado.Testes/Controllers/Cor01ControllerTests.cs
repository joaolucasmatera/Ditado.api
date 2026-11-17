using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.DTOs.Professores;
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
    public async Task ErroArquivadoClassificaNaAplicacaoExistenteEBloqueiaNovoUso()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioAsync(TipoUsuario.Aluno);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        await GarantirEscolaEVinculoAsync(aluno.Id, PapelEscolar.Aluno);
        var erroId = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.ComplexidadeSilabica));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var turma = new Turma { Nome = "Turma com aplicação", EscolaId = escolaId, ProfessorResponsavelId = professor.Id };
        var novaTurma = new Turma { Nome = "Turma sem aplicação", EscolaId = escolaId, ProfessorResponsavelId = professor.Id };
        turma.Alunos.Add(await db.Usuarios.SingleAsync(u => u.Id == aluno.Id));
        db.Turmas.AddRange(turma, novaTurma);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(professor.Token);
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", Pedido([erroId])));
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        var atribuicao = new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(3) };
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados", atribuicao)).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var realizacao = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
        var lacunas = realizacao.Segmentos.Where(s => s.SegmentoId.HasValue).Select(s => s.SegmentoId!.Value).ToArray();
        var pedido = new SubmeterRespostaRequest
        {
            Respostas = [new() { SegmentoId = lacunas[1], Resposta = "pato" }, new() { SegmentoId = lacunas[2], Resposta = "gato" }]
        };
        var anterior = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter", pedido));
        var classificacao = Assert.Single(anterior.Detalhes[0].Classificacoes);
        Assert.Equal(erroId, classificacao.ErroCatalogoId);
        Assert.Equal((int)TipoErro.ComplexidadeSilabica, classificacao.TipoErro);

        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/ErrosCatalogo/{erroId}/arquivar", new { })).StatusCode);
        AdicionarTokenAutorizacao(professor.Token);
        var catalogo = Assert.IsType<List<ErroPublicadoResponse>>(await GetAsync<List<ErroPublicadoResponse>>("/api/ErrosCatalogo?estado=ativo"));
        Assert.DoesNotContain(catalogo, erro => erro.Id == erroId);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsyncRaw("/api/Ditados", Pedido([erroId]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsyncRaw($"/api/Turmas/{novaTurma.Id}/ditados", atribuicao)).StatusCode);

        AdicionarTokenAutorizacao(aluno.Token);
        var nova = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter", pedido));
        Assert.Equal(50m, nova.Nota);
        Assert.Equal(classificacao, Assert.Single(nova.Detalhes[0].Classificacoes));

        var aplicacao = await db.AplicacoesDitados.AsNoTracking().SingleAsync(a => a.DitadoId == ditado.Id);
        var tentativas = await db.RespostaDitados.AsNoTracking().Where(r => r.DitadoId == ditado.Id)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.TiposErro).ToListAsync();
        Assert.Equal(2, tentativas.Count);
        Assert.All(tentativas, tentativa =>
        {
            Assert.Equal(aplicacao.Id, tentativa.AplicacaoDitadoId);
            Assert.Equal(escolaId, tentativa.EscolaId);
            Assert.Equal(50m, tentativa.Nota);
            var tipo = Assert.Single(tentativa.RespostasSegmentos.SelectMany(s => s.TiposErro));
            Assert.Equal(erroId, tipo.ErroCatalogoId);
            Assert.Equal(TipoErro.ComplexidadeSilabica, tipo.TipoErro);
        });
    }

    [Fact]
    public async Task DitadoLegadoSemSelecaoCalculaNotaEConservaClassificacaoHistorica()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioAsync(TipoUsuario.Aluno);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var lacuna = new DitadoSegmento { Tipo = TipoSegmento.Lacuna, Conteudo = "café", Ordem = 1 };
        var outraLacuna = new DitadoSegmento { Tipo = TipoSegmento.Lacuna, Conteudo = "gato", Ordem = 2 };
        var ditado = new Dominio.Entidades.Ditado { Titulo = "Legado", AutorId = professor.Id, Segmentos = [lacuna, outraLacuna] };
        ditado.Publicacoes.Add(new PublicacaoDitado { Nivel = NivelVisibilidade.PublicoGeral, Versao = 1, ChaveVigente = 1, CriadaPorId = professor.Id });
        var turma = new Turma { Nome = "Turma legada", ProfessorResponsavelId = professor.Id };
        turma.Alunos.Add(await db.Usuarios.SingleAsync(u => u.Id == aluno.Id));
        db.TurmaDitados.Add(new TurmaDitado { Turma = turma, Ditado = ditado, DataLimite = DateTime.UtcNow.AddDays(1) });
        var historica = new RespostaDitado { Ditado = ditado, AlunoId = aluno.Id, Nota = 75m, DataRealizacao = DateTime.UtcNow.AddDays(-1) };
        historica.RespostasSegmentos.Add(new RespostaSegmento
        {
            Segmento = lacuna, RespostaFornecida = "cafe", RespostaEsperada = "café",
            EstadoClassificacao = EstadoClassificacao.Classificado, MetodoClassificacao = MetodoClassificacao.Codigo,
            TiposErro = [new RespostaSegmentoTipoErro { TipoErro = TipoErro.Acentuacao }]
        });
        db.RespostaDitados.Add(historica);
        await db.SaveChangesAsync();

        AdicionarTokenAutorizacao(aluno.Token);
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.OK, (await GetAsyncRaw($"{url}/realizar")).StatusCode);
        var resultado = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter",
            new SubmeterRespostaRequest
            {
                Respostas = [new() { SegmentoId = lacuna.Id, Resposta = "cafe" }, new() { SegmentoId = outraLacuna.Id, Resposta = "gato" }]
            }));
        Assert.Equal(50m, resultado.Nota);
        Assert.Equal(1, resultado.Acertos);
        Assert.Equal(nameof(EstadoClassificacao.NaoClassificado), resultado.Detalhes[0].EstadoClassificacao);
        Assert.Null(resultado.Detalhes[0].MetodoClassificacao);
        Assert.Empty(resultado.Detalhes[0].TiposErro);
        Assert.Empty(resultado.Detalhes[0].Classificacoes);
        Assert.Equal(nameof(EstadoClassificacao.NaoSeAplica), resultado.Detalhes[1].EstadoClassificacao);

        var tentativas = await db.RespostaDitados.AsNoTracking().Where(r => r.DitadoId == ditado.Id)
            .Include(r => r.RespostasSegmentos).ThenInclude(r => r.TiposErro).ToListAsync();
        Assert.Equal(2, tentativas.Count);
        var nova = Assert.Single(tentativas, r => r.Id == resultado.RespostaDitadoId);
        Assert.All(nova.RespostasSegmentos, segmento => Assert.Empty(segmento.TiposErro));
        Assert.Equal(EstadoClassificacao.NaoClassificado, Assert.Single(nova.RespostasSegmentos, s => s.SegmentoId == lacuna.Id).EstadoClassificacao);
        var anterior = Assert.Single(tentativas, r => r.Id == historica.Id);
        Assert.Equal(75m, anterior.Nota);
        var segmentoHistorico = Assert.Single(anterior.RespostasSegmentos);
        Assert.Equal(EstadoClassificacao.Classificado, segmentoHistorico.EstadoClassificacao);
        Assert.Equal(MetodoClassificacao.Codigo, segmentoHistorico.MetodoClassificacao);
        var tipoHistorico = Assert.Single(segmentoHistorico.TiposErro);
        Assert.Equal(TipoErro.Acentuacao, tipoHistorico.TipoErro);
        Assert.Null(tipoHistorico.ErroCatalogoId);

        AdicionarTokenAutorizacao(professor.Token);
        var relatorio = Assert.IsType<DitadoAtribuidoDetalheDto>(await GetAsync<DitadoAtribuidoDetalheDto>(
            $"/api/Professores/turmas/{turma.Id}/ditados/{ditado.Id}/resultados"));
        var tipoNoRelatorio = Assert.Single(relatorio.ErrosPorTipo);
        Assert.Equal((int)TipoErro.Acentuacao, tipoNoRelatorio.TipoErroId);
        Assert.Equal(1, tipoNoRelatorio.Quantidade);
    }

    [Fact]
    public async Task SwaggerExplicitaSelecaoEIdentidadeDaClassificacao()
    {
        var swagger = await Client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var schemas = swagger.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("CriarDitadoRequest").GetProperty("properties").TryGetProperty("erroCatalogoIds", out _));
        Assert.True(schemas.GetProperty("DitadoCompletoDto").GetProperty("properties").TryGetProperty("possuiRespostas", out _));
        Assert.True(schemas.GetProperty("DetalheRespostaDto").GetProperty("properties").TryGetProperty("classificacoes", out _));
        var submissao = swagger.GetProperty("paths").GetProperty("/api/Ditados/{id}/submeter").GetProperty("post");
        Assert.Contains("legados sem seleção", submissao.GetProperty("description").GetString());
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
