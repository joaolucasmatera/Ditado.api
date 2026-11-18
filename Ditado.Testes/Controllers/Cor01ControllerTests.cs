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
    public async Task CadastroComNomesLongosGeraCodigosDistintosEPermiteAcessoDosDoisAlunos()
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var legado = await CriarUsuarioAsync(TipoUsuario.Aluno);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var escola = await db.Escolas.SingleAsync(e => e.Id == escolaId);
        escola.Nome = "Escola Municipal de Ensino Fundamental com Nome Extenso para Regressão";
        var alunoLegado = await db.Usuarios.SingleAsync(u => u.Id == legado.Id);
        alunoLegado.CodigoCadastro = "EscolaAntigaTurmaAntiga001";
        await db.SaveChangesAsync();
        var regraId = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.Acentuacao));
        AdicionarTokenAutorizacao(professor.Token);
        var turma = Assert.IsType<Ditado.Aplicacao.DTOs.Turmas.TurmaResponse>(
            await PostAsync<Ditado.Aplicacao.DTOs.Turmas.TurmaResponse>("/api/Turmas",
                new { nome = "Turma do Ensino Fundamental com Nome Extenso para Regressão", serie = 3, anoLetivo = 2026, escolaId, professorResponsavelId = professor.Id }));
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", Pedido([regraId])));
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados",
            new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(3) })).StatusCode);
        var alunos = new List<UsuarioResponse>();
        foreach (var chamada in new[] { 1, 999 })
        {
            var response = await PostAsyncRaw("/api/Usuarios/alunos-manual", new
            {
                nome = $"Aluno da chamada {chamada}", numeroChamada = chamada, turmaId = turma.Id
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            alunos.Add(Assert.IsType<UsuarioResponse>(await response.Content.ReadFromJsonAsync<UsuarioResponse>()));
        }
        Assert.NotEqual(alunos[0].CodigoCadastro, alunos[1].CodigoCadastro);
        Assert.All(alunos, aluno => Assert.InRange(aluno.CodigoCadastro!.Length, 1, 32));
        Assert.Equal($"E{escolaId}-T{turma.Id}-N001", alunos[0].CodigoCadastro);
        Assert.Equal($"E{escolaId}-T{turma.Id}-N999", alunos[1].CodigoCadastro);
        Assert.Equal("EscolaAntigaTurmaAntiga001", await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == legado.Id).Select(u => u.CodigoCadastro).SingleAsync());
        RemoverTokenAutorizacao();
        var acessoLegado = new { matriculaOuCodigo = "EscolaAntigaTurmaAntiga001", pin = "3579" };
        Assert.Equal(HttpStatusCode.OK, (await PostAsyncRaw("/api/Usuarios/primeiro-acesso-aluno", acessoLegado)).StatusCode);
        var loginLegado = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/Usuarios/login-aluno", acessoLegado));
        Assert.Equal(legado.Id, loginLegado.Usuario.Id);

        foreach (var aluno in alunos)
        {
            RemoverTokenAutorizacao();
            var acesso = new { matriculaOuCodigo = aluno.CodigoCadastro, pin = "1357" };
            Assert.Equal(HttpStatusCode.OK, (await PostAsyncRaw("/api/Usuarios/primeiro-acesso-aluno", acesso)).StatusCode);
            var login = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/Usuarios/login-aluno", acesso));
            Assert.Equal(aluno.Id, login.Usuario.Id);
            AdicionarTokenAutorizacao(login.Token);
            var realizar = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
            var segmentoId = realizar.Segmentos.First(s => s.SegmentoId.HasValue).SegmentoId!.Value;
            var resultado = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter",
                new SubmeterRespostaRequest { Respostas = [new() { SegmentoId = segmentoId, Resposta = "cafe" }] }));
            Assert.Equal(regraId, Assert.Single(resultado.Detalhes.Single(d => d.SegmentoId == segmentoId).Classificacoes).ErroCatalogoId);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CadastroManualPermitePrimeiroAcessoESubmissaoSemCompletarVinculos(bool ingressoAposAtribuicao)
    {
        var professor = await CriarUsuarioAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        var regraId = Assert.Single(await GarantirRegrasPublicasAsync(TipoErro.Acentuacao));
        AdicionarTokenAutorizacao(professor.Token);
        var turma = Assert.IsType<Ditado.Aplicacao.DTOs.Turmas.TurmaResponse>(
            await PostAsync<Ditado.Aplicacao.DTOs.Turmas.TurmaResponse>("/api/Turmas",
                new { nome = $"Turma de ingresso {professor.Id}", serie = 3, anoLetivo = 2026, escolaId, professorResponsavelId = professor.Id }));
        var ditado = Assert.IsType<DitadoResponse>(await PostAsync<DitadoResponse>("/api/Ditados", Pedido([regraId])));
        var url = $"/api/Ditados/{ditado.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"{url}/visibilidade", new { nivel = 1 })).StatusCode);
        var atribuicao = new { ditadoId = ditado.Id, dataLimite = DateTime.UtcNow.AddDays(3) };
        if (ingressoAposAtribuicao)
            Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados", atribuicao)).StatusCode);

        var cadastro = await PostAsyncRaw("/api/Usuarios/alunos-manual", new
        {
            nome = "Aluno cadastrado pelo professor", numeroChamada = 1, turmaId = turma.Id
        });
        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);
        var aluno = Assert.IsType<UsuarioResponse>(await cadastro.Content.ReadFromJsonAsync<UsuarioResponse>());
        if (!ingressoAposAtribuicao)
            Assert.Equal(HttpStatusCode.NoContent, (await PostAsyncRaw($"/api/Turmas/{turma.Id}/ditados", atribuicao)).StatusCode);

        RemoverTokenAutorizacao();
        var primeiroAcesso = new { matriculaOuCodigo = aluno.CodigoCadastro, pin = "1357" };
        Assert.Equal(HttpStatusCode.OK, (await PostAsyncRaw("/api/Usuarios/primeiro-acesso-aluno/verificar",
            new { matriculaOuCodigo = aluno.CodigoCadastro })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsyncRaw("/api/Usuarios/primeiro-acesso-aluno", primeiroAcesso)).StatusCode);
        var login = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/Usuarios/login-aluno", primeiroAcesso));
        AdicionarTokenAutorizacao(login.Token);
        var realizar = Assert.IsType<DitadoParaRealizarResponse>(await GetAsync<DitadoParaRealizarResponse>($"{url}/realizar"));
        var segmentoId = realizar.Segmentos.First(s => s.SegmentoId.HasValue).SegmentoId!.Value;
        var resultado = Assert.IsType<ResultadoDitadoResponse>(await PostAsync<ResultadoDitadoResponse>($"{url}/submeter",
            new SubmeterRespostaRequest { Respostas = [new() { SegmentoId = segmentoId, Resposta = "cafe" }] }));
        Assert.Equal(regraId, Assert.Single(resultado.Detalhes.Single(d => !d.RespostaAusente).Classificacoes).ErroCatalogoId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var vinculo = await db.VinculosEscolares.AsNoTracking().SingleAsync(v => v.UsuarioId == aluno.Id);
        Assert.True(vinculo.Ativo);
        Assert.Equal(escolaId, vinculo.EscolaId);
        Assert.Equal(PapelEscolar.Aluno, vinculo.Papel);
        var participacao = await db.ParticipacoesAplicacoes.AsNoTracking().Include(p => p.AplicacaoDitado)
            .SingleAsync(p => p.AlunoId == aluno.Id);
        Assert.Null(participacao.RevogadaEm);
        Assert.Equal(ditado.Id, participacao.AplicacaoDitado.DitadoId);
        Assert.Equal(escolaId, participacao.AplicacaoDitado.EscolaId);
        var tentativa = await db.RespostaDitados.SingleAsync(r => r.Id == resultado.RespostaDitadoId);
        Assert.Equal(participacao.AplicacaoDitadoId, tentativa.AplicacaoDitadoId);
        Assert.Equal(escolaId, tentativa.EscolaId);
    }

    [Theory]
    [InlineData(TipoUsuario.Administrador, false)]
    [InlineData(TipoUsuario.Administrador, true)]
    [InlineData(TipoUsuario.Professor, false)]
    [InlineData(TipoUsuario.Professor, true)]
    public async Task CadastroManualRecusaAtorSemEscopoSemGravarAluno(TipoUsuario tipo, bool vinculado)
    {
        var responsavel = await CriarUsuarioAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(responsavel.Id);
        var ator = await CriarUsuarioAsync(tipo);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var turma = new Turma
        {
            Nome = "Turma protegida", EscolaId = escolaId,
            ProfessorResponsavelId = tipo == TipoUsuario.Professor && !vinculado ? ator.Id : responsavel.Id
        };
        db.Turmas.Add(turma);
        if (vinculado)
        {
            var outraEscola = new Escola { Nome = "Outra escola", Codigo = Guid.NewGuid().ToString("N") };
            db.VinculosEscolares.Add(new VinculoEscolar
            {
                UsuarioId = ator.Id, Escola = outraEscola,
                Papel = tipo == TipoUsuario.Administrador ? PapelEscolar.Administrador : PapelEscolar.Professor
            });
        }
        await db.SaveChangesAsync();
        var antes = await db.Usuarios.CountAsync();
        AdicionarTokenAutorizacao(ator.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsyncRaw("/api/Usuarios/alunos-manual",
            new { nome = "Cadastro recusado", numeroChamada = 1, turmaId = turma.Id })).StatusCode);
        Assert.Equal(antes, await db.Usuarios.CountAsync());
    }

    [Theory]
    [InlineData(PapelEscolar.Administrador, true, true, HttpStatusCode.OK)]
    [InlineData(PapelEscolar.Professor, true, true, HttpStatusCode.Forbidden)]
    [InlineData(PapelEscolar.Administrador, false, true, HttpStatusCode.Forbidden)]
    [InlineData(PapelEscolar.Administrador, true, false, HttpStatusCode.Forbidden)]
    public async Task CadastroManualEmTurmaDeOutroProfessorRespeitaPapelEEscola(
        PapelEscolar papel, bool mesmaEscola, bool vinculoAtivo, HttpStatusCode esperado)
    {
        var responsavel = await CriarUsuarioAsync(TipoUsuario.Professor);
        var login = $"admin-escolar-{Guid.NewGuid():N}@teste.local";
        RemoverTokenAutorizacao();
        var ator = Assert.IsType<UsuarioResponse>(await PostAsync<UsuarioResponse>("/api/Usuarios/cadastro-professor",
            new { nome = "Administrador escolar", login, senha = "senha123", opcaoEscola = "CriarNova", nomeNovaEscola = "Escola do administrador" }));
        Assert.Equal(nameof(TipoUsuario.Professor), ator.Tipo);
        var sessao = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/Usuarios/login",
            new { login, senha = "senha123" }));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var vinculo = await db.VinculosEscolares.SingleAsync(v => v.UsuarioId == ator.Id);
        Assert.Equal(PapelEscolar.Administrador, vinculo.Papel);
        Assert.True(vinculo.Ativo);
        var escolaId = mesmaEscola ? vinculo.EscolaId : await GarantirEscolaEVinculoAsync(responsavel.Id);
        if (mesmaEscola)
            db.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = responsavel.Id, EscolaId = escolaId, Papel = PapelEscolar.Professor });
        vinculo.Papel = papel;
        vinculo.Ativo = vinculoAtivo;
        var turma = new Turma { Nome = "Turma de outro professor", EscolaId = escolaId, ProfessorResponsavelId = responsavel.Id };
        db.Turmas.Add(turma);
        await db.SaveChangesAsync();
        var antes = await db.Usuarios.CountAsync();
        AdicionarTokenAutorizacao(sessao.Token);
        var response = await PostAsyncRaw("/api/Usuarios/alunos-manual",
            new { nome = "Aluno cadastrado pelo administrador", numeroChamada = 1, turmaId = turma.Id });
        Assert.Equal(esperado, response.StatusCode);
        Assert.Equal(antes + (esperado == HttpStatusCode.OK ? 1 : 0), await db.Usuarios.CountAsync());
        if (esperado == HttpStatusCode.OK)
        {
            var aluno = Assert.IsType<UsuarioResponse>(await response.Content.ReadFromJsonAsync<UsuarioResponse>());
            Assert.True(await db.VinculosEscolares.AnyAsync(v => v.UsuarioId == aluno.Id && v.EscolaId == escolaId && v.Ativo && v.Papel == PapelEscolar.Aluno));
            Assert.True(await db.Turmas.AnyAsync(t => t.Id == turma.Id && t.Alunos.Any(a => a.Id == aluno.Id)));
        }
    }

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
        Assert.Equal(33.33m, resultado.Nota);
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
        var classificacao = Assert.Single(anterior.Detalhes.Single(d => d.SegmentoId == lacunas[1]).Classificacoes);
        Assert.True(anterior.Detalhes.Single(d => d.SegmentoId == lacunas[0]).RespostaAusente);
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
        Assert.Equal(33.33m, nova.Nota);
        Assert.Equal(classificacao, Assert.Single(nova.Detalhes.Single(d => d.SegmentoId == lacunas[1]).Classificacoes));

        var aplicacao = await db.AplicacoesDitados.AsNoTracking().SingleAsync(a => a.DitadoId == ditado.Id);
        var tentativas = await db.RespostaDitados.AsNoTracking().Where(r => r.DitadoId == ditado.Id)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.TiposErro).ToListAsync();
        Assert.Equal(2, tentativas.Count);
        Assert.All(tentativas, tentativa =>
        {
            Assert.Equal(aplicacao.Id, tentativa.AplicacaoDitadoId);
            Assert.Equal(escolaId, tentativa.EscolaId);
            Assert.Equal(nova.Nota, tentativa.Nota);
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
        var cadastro = swagger.GetProperty("paths").GetProperty("/api/Usuarios/alunos-manual").GetProperty("post");
        Assert.Contains("E12-T34-N003", cadastro.GetProperty("description").GetString());
        Assert.Contains("inclusive conta de tipo Professor", cadastro.GetProperty("description").GetString());
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
