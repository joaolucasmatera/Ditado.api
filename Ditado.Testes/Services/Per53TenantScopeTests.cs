using Ditado.Aplicacao.Services;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class Per53TenantScopeTests : IDisposable
{
    private readonly DitadoDbContext _context;
    private readonly DitadoService _ditados;
    private readonly UsuarioService _usuarios;
    private readonly TurmaService _turmas;
    private readonly MarketplaceService _marketplace;

    public Per53TenantScopeTests()
    {
        _context = new DitadoDbContext(new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _ditados = new DitadoService(_context, new CategoriaService(_context));
        _turmas = new TurmaService(_context);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ChaveSecreta"] = "chave-de-teste-com-tamanho-suficiente-123456",
            ["Jwt:Emissor"] = "teste", ["Jwt:Audiencia"] = "teste"
        }).Build();
        _usuarios = new UsuarioService(_context, new PasswordHasher(), new TokenService(config));
        var visibilidade = new VisibilidadeService(_context);
        _marketplace = new MarketplaceService(_context, visibilidade);
    }

    [Fact]
    public async Task ProfessorVinculadoVeSomenteUsuariosDaPropriaEscola()
    {
        var (professor, escolaA, escolaB) = await CriarCenarioAsync();
        var outro = new Usuario { Nome = "Outro", Login = "outro@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.Add(outro);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = outro.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();

        var resultado = await _usuarios.ListarUsuariosAsync(professor.Id, TipoUsuario.Professor);

        Assert.Contains(resultado, u => u.Id == professor.Id);
        Assert.DoesNotContain(resultado, u => u.Id == outro.Id);
    }

    [Fact]
    public async Task DitadoDoProprioProfessorApareceMesmoSemPublicacaoEOutraEscolaFicaOculta()
    {
        var (professor, escolaA, escolaB) = await CriarCenarioAsync();
        var outroProfessor = new Usuario { Nome = "Outro", Login = "outro2@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.Add(outroProfessor);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = outroProfessor.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        var proprio = new Ditado.Dominio.Entidades.Ditado { Titulo = "Próprio", AudioLeitura = [1], AutorId = professor.Id, Ativo = true };
        var outro = new Ditado.Dominio.Entidades.Ditado { Titulo = "Outra escola", AudioLeitura = [2], AutorId = outroProfessor.Id, Ativo = true };
        _context.Ditados.AddRange(proprio, outro);
        await _context.SaveChangesAsync();
        _context.PublicacoesDitados.Add(new PublicacaoDitado { DitadoId = outro.Id, Nivel = NivelVisibilidade.Instituicao, EscolaId = escolaB.Id, Versao = 1, ChaveVigente = 1, CriadaPorId = outroProfessor.Id });
        await _context.SaveChangesAsync();

        var resultado = await _ditados.ListarDitadosAsync(professor.Id, TipoUsuario.Professor);

        Assert.Contains(resultado, d => d.Id == proprio.Id);
        Assert.DoesNotContain(resultado, d => d.Id == outro.Id);
    }

    [Fact]
    public async Task AdministradorGeralVeAsDuasEscolasEContaSemEscopoNaoAmpliaAcesso()
    {
        var (professor, escolaA, escolaB) = await CriarCenarioAsync();
        var adminGlobal = new Usuario { Nome = "Global", Login = "global@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true };
        var semEscopo = new Usuario { Nome = "Sem escopo", Login = "sem@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.AddRange(adminGlobal, semEscopo);
        await _context.SaveChangesAsync();
        var d1 = new Ditado.Dominio.Entidades.Ditado { Titulo = "A", AudioLeitura = [1], AutorId = professor.Id };
        var d2 = new Ditado.Dominio.Entidades.Ditado { Titulo = "B", AudioLeitura = [2], AutorId = adminGlobal.Id };
        _context.Ditados.AddRange(d1, d2);
        await _context.SaveChangesAsync();

        var global = await _ditados.ListarDitadosAsync(adminGlobal.Id, TipoUsuario.Administrador);
        var isolado = await _ditados.ListarDitadosAsync(semEscopo.Id, TipoUsuario.Professor);

        Assert.Equal(2, global.Count);
        Assert.Empty(isolado);
    }

    [Fact]
    public async Task AdministradorDaEscolaNaoVeDitadoDeOutroTenant()
    {
        var (professor, escolaA, escolaB) = await CriarCenarioAsync();
        var admin = new Usuario { Nome = "Admin A", Login = "admin-a@teste", Tipo = TipoUsuario.Administrador, Ativo = true };
        var autorB = new Usuario { Nome = "Autor B", Login = "autor-b@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.AddRange(admin, autorB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.AddRange(
            new VinculoEscolar { UsuarioId = admin.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Administrador, Ativo = true },
            new VinculoEscolar { UsuarioId = autorB.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        var daA = new Ditado.Dominio.Entidades.Ditado { Titulo = "A", AudioLeitura = [1], AutorId = professor.Id };
        var daB = new Ditado.Dominio.Entidades.Ditado { Titulo = "B", AudioLeitura = [2], AutorId = autorB.Id };
        _context.AddRange(daA, daB);
        await _context.SaveChangesAsync();
        _context.PublicacoesDitados.AddRange(
            new PublicacaoDitado { DitadoId = daA.Id, Nivel = NivelVisibilidade.Instituicao, EscolaId = escolaA.Id, Versao = 1, ChaveVigente = 1, CriadaPorId = professor.Id },
            new PublicacaoDitado { DitadoId = daB.Id, Nivel = NivelVisibilidade.Instituicao, EscolaId = escolaB.Id, Versao = 1, ChaveVigente = 1, CriadaPorId = autorB.Id });
        await _context.SaveChangesAsync();

        var resultado = await _ditados.ListarDitadosAsync(admin.Id, TipoUsuario.Administrador);

        Assert.Contains(resultado, d => d.Id == daA.Id);
        Assert.DoesNotContain(resultado, d => d.Id == daB.Id);
    }

    [Fact]
    public async Task CatalogoRecusaPublicacaoPorContaSemVinculo()
    {
        var semEscopo = new Usuario { Nome = "Sem escopo", Login = "sem-catalogo@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.Add(semEscopo);
        await _context.SaveChangesAsync();
        var marketplace = new MarketplaceService(_context, new VisibilidadeService(_context));

        await Assert.ThrowsAsync<InvalidOperationException>(() => marketplace.PublicarAsync(
            new PublicarErroRequest { Nome = "Erro", Descricao = "Definição" }, semEscopo.Id));
    }

    [Fact]
    public async Task AdministradorDaEscolaNaoAtualizaUsuarioDeOutraEscola()
    {
        var (_, escolaA, escolaB) = await CriarCenarioAsync();
        var adminA = new Usuario { Nome = "Admin A", Login = "admin-a-update@teste", Tipo = TipoUsuario.Administrador, Ativo = true };
        var alvoB = new Usuario { Nome = "Alvo B", Login = "alvo-b-update@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.AddRange(adminA, alvoB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.AddRange(
            new VinculoEscolar { UsuarioId = adminA.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Administrador, Ativo = true },
            new VinculoEscolar { UsuarioId = alvoB.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();

        var act = () => _usuarios.AtualizarUsuarioAsync(alvoB.Id,
            new Ditado.Aplicacao.DTOs.Usuarios.AtualizarUsuarioRequest { Nome = "Tentativa" }, adminA.Id, TipoUsuario.Administrador);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task AdministradorDaEscolaNaoAprovaSolicitacaoDeOutraEscola()
    {
        var (_, escolaA, escolaB) = await CriarCenarioAsync();
        var adminA = new Usuario { Nome = "Admin A", Login = "admin-a-approve@teste", Tipo = TipoUsuario.Administrador, Ativo = true };
        var pendenteB = new Usuario { Nome = "Pendente B", Login = "pendente-b@teste", Tipo = TipoUsuario.AcessoSolicitado, Ativo = false };
        _context.AddRange(adminA, pendenteB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.AddRange(
            new VinculoEscolar { UsuarioId = adminA.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Administrador, Ativo = true },
            new VinculoEscolar { UsuarioId = pendenteB.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = false });
        await _context.SaveChangesAsync();

        var act = () => _usuarios.AprovarAcessoAsync(pendenteB.Id,
            new Ditado.Aplicacao.DTOs.Usuarios.AprovarAcessoRequest { NovoTipo = TipoUsuario.Professor }, adminA.Id, TipoUsuario.Administrador);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ProfessorNaoListaTurmasDeOutroProfessor()
    {
        var (professorA, escolaA, escolaB) = await CriarCenarioAsync();
        var professorB = new Usuario { Nome = "Professor B", Login = "prof-b-turma@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.Add(professorB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professorB.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        var turmaB = new Turma { Nome = "Turma B", Serie = 3, AnoLetivo = 2026, EscolaId = escolaB.Id, ProfessorResponsavelId = professorB.Id, Ativo = true };
        _context.Turmas.Add(turmaB);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _turmas.ListarTurmasPorProfessorAsync(professorB.Id, professorA.Id, TipoUsuario.Professor));
    }

    [Fact]
    public async Task AdministradorDaEscolaNaoDeletaDitadoDeOutraEscola()
    {
        var (_, escolaA, escolaB) = await CriarCenarioAsync();
        var adminA = new Usuario { Nome = "Admin A", Login = "admin-a-delete@teste", Tipo = TipoUsuario.Administrador, Ativo = true };
        var autorB = new Usuario { Nome = "Autor B", Login = "autor-b-delete@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.AddRange(adminA, autorB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.AddRange(
            new VinculoEscolar { UsuarioId = adminA.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Administrador, Ativo = true },
            new VinculoEscolar { UsuarioId = autorB.Id, EscolaId = escolaB.Id, Papel = PapelEscolar.Professor, Ativo = true });
        var ditadoB = new Ditado.Dominio.Entidades.Ditado { Titulo = "Ditado B", AudioLeitura = [1], AutorId = autorB.Id, Ativo = true };
        _context.Ditados.Add(ditadoB);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _ditados.DeletarDitadoAsync(ditadoB.Id, adminA.Id, TipoUsuario.Administrador));
        Assert.NotNull(await _context.Ditados.FindAsync(ditadoB.Id));
    }

    [Fact]
    public async Task ProfessorPodeAtualizarErroProprioEOutroProfessorNaoPode()
    {
        var (professorA, escolaA, _) = await CriarCenarioAsync();
        var erro = await _marketplace.PublicarAsync(new PublicarErroRequest { Nome = "Original", Descricao = "Descrição" }, professorA.Id);
        var atualizado = await _marketplace.AtualizarAsync(new PublicarErroRequest { Nome = "Atualizado", Descricao = "Descrição nova" }, erro.Id, professorA.Id, TipoUsuario.Professor);
        Assert.Equal("Atualizado", atualizado!.Nome);

        var professorB = new Usuario { Nome = "Professor B", Login = "prof-b-erro@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.Usuarios.Add(professorB);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professorB.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _marketplace.AtualizarAsync(
            new PublicarErroRequest { Nome = "Invasão", Descricao = "Não permitido" }, erro.Id, professorB.Id, TipoUsuario.Professor));
    }

    [Fact]
    public async Task ErroUsadoEmDitadoFicaImutavel()
    {
        var (professor, escolaA, _) = await CriarCenarioAsync();
        var erro = await _marketplace.PublicarAsync(new PublicarErroRequest { Nome = "Usado", Descricao = "Descrição" }, professor.Id);
        var aplicacao = new AplicacaoDitado { EscolaId = escolaA.Id, TurmaId = 1, DitadoId = 1 };
        _context.AplicacoesDitados.Add(aplicacao);
        await _context.SaveChangesAsync();
        _context.AplicacoesErrosCatalogo.Add(new AplicacaoErroCatalogo { AplicacaoDitadoId = aplicacao.Id, ErroCatalogoId = erro.Id, CodigoSnapshot = erro.Codigo, NomeSnapshot = erro.Nome });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _marketplace.AtualizarAsync(
            new PublicarErroRequest { Nome = "Tentativa", Descricao = "Bloqueada" }, erro.Id, professor.Id, TipoUsuario.Professor));
    }

    [Fact]
    public async Task CatalogoDoAutorIncluiErroSemPublicacaoESeparaArquivadosComAutoriaEPrivacidade()
    {
        var (professor, _, _) = await CriarCenarioAsync();
        var ativo = await _marketplace.PublicarAsync(new PublicarErroRequest { Nome = "Ativo", Descricao = "Descrição" }, professor.Id);
        var arquivado = await _marketplace.PublicarAsync(new PublicarErroRequest { Nome = "Arquivado", Descricao = "Descrição" }, professor.Id);
        var entidadeArquivada = await _context.ErrosCatalogo.SingleAsync(e => e.Id == arquivado.Id);
        entidadeArquivada.Ativo = false;
        entidadeArquivada.ArquivadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var ativos = await _marketplace.ListarAsync(professor.Id, TipoUsuario.Professor, "ativo");
        var arquivados = await _marketplace.ListarAsync(professor.Id, TipoUsuario.Professor, "arquivado");

        var itemAtivo = Assert.Single(ativos, e => e.Id == ativo.Id);
        Assert.Equal("Professor", itemAtivo.AutorNome);
        Assert.Equal("Instituicao", itemAtivo.NivelVisibilidade);
        Assert.DoesNotContain(ativos, e => e.Id == arquivado.Id);
        Assert.Contains(arquivados, e => e.Id == arquivado.Id);
        Assert.DoesNotContain(arquivados, e => e.Id == ativo.Id);
    }

    [Fact]
    public async Task ListagemDeDitadosRetornaNivelDePrivacidadeDaPublicacaoVigente()
    {
        var (professor, escolaA, _) = await CriarCenarioAsync();
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Privado", AudioLeitura = [1], AutorId = professor.Id, Ativo = true };
        _context.Ditados.Add(ditado);
        await _context.SaveChangesAsync();
        _context.PublicacoesDitados.Add(new PublicacaoDitado
        {
            DitadoId = ditado.Id, Nivel = NivelVisibilidade.Instituicao, EscolaId = escolaA.Id,
            Versao = 1, ChaveVigente = 1, CriadaPorId = professor.Id
        });
        await _context.SaveChangesAsync();

        var resultado = await _ditados.ListarDitadosAsync(professor.Id, TipoUsuario.Professor);

        Assert.Equal("Instituicao", Assert.Single(resultado, d => d.Id == ditado.Id).NivelVisibilidade);
    }

    private async Task<(Usuario professor, Escola escolaA, Escola escolaB)> CriarCenarioAsync()
    {
        var escolaA = new Escola { Nome = "A", Codigo = "A", Ativa = true };
        var escolaB = new Escola { Nome = "B", Codigo = "B", Ativa = true };
        var professor = new Usuario { Nome = "Professor", Login = "prof@teste", Tipo = TipoUsuario.Professor, Ativo = true };
        _context.AddRange(escolaA, escolaB, professor);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professor.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();
        return (professor, escolaA, escolaB);
    }

    public void Dispose() => _context.Dispose();
}
