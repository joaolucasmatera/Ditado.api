using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class UsuarioServiceMt04Tests : IDisposable
{
    private readonly DitadoDbContext _context;
    private readonly UsuarioService _service;

    public UsuarioServiceMt04Tests()
    {
        var options = new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DitadoDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ChaveSecreta"] = "chave-de-teste-com-tamanho-suficiente-123456",
                ["Jwt:Emissor"] = "teste",
                ["Jwt:Audiencia"] = "teste"
            })
            .Build();

        _service = new UsuarioService(_context, new PasswordHasher(), new TokenService(configuration));
    }

    [Fact]
    public async Task ContextoEscolar_ComVinculoAtivo_DeveAutorizarEAuditar()
    {
        var usuario = await CriarUsuarioComEscolaAsync(true);

        var contextos = await _service.ListarContextosEscolaresAsync(usuario.Id);
        var autorizado = await _service.PossuiAcessoEscolarAsync(usuario.Id, 7);
        await _service.RegistrarAuditoriaAsync(usuario.Id, 7, "SelecionarContextoEscolar", "Escola", "Permitido", "Vínculo escolar ativo.");

        Assert.Single(contextos);
        Assert.True(autorizado);
        Assert.Equal("Permitido", await _context.AuditoriaEventos.Select(a => a.Resultado).SingleAsync());
    }

    [Fact]
    public async Task ContextoEscolar_SemVinculoAtivo_DeveNegarEAuditar()
    {
        var usuario = await CriarUsuarioComEscolaAsync(false);

        var autorizado = await _service.PossuiAcessoEscolarAsync(usuario.Id, 7);
        await _service.RegistrarAuditoriaAsync(usuario.Id, 7, "SelecionarContextoEscolar", "Escola", "Negado", "Usuário sem vínculo escolar ativo.");

        Assert.False(autorizado);
        Assert.Equal("Negado", await _context.AuditoriaEventos.Select(a => a.Resultado).SingleAsync());
    }

    [Fact]
    public async Task RevogarSessao_DevePersistirRevogacaoEAuditoria()
    {
        var usuario = await CriarUsuarioComEscolaAsync(true);
        var sessaoId = Guid.NewGuid();
        _context.SessoesUsuarios.Add(new SessaoUsuario
        {
            Id = sessaoId,
            UsuarioId = usuario.Id,
            CriadaEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.AddHours(1)
        });
        await _context.SaveChangesAsync();

        Assert.True(await _service.RevogarSessaoAsync(sessaoId, usuario.Id, "teste"));

        var sessao = await _context.SessoesUsuarios.SingleAsync();
        Assert.NotNull(sessao.RevogadaEm);
        Assert.Equal("Permitido", await _context.AuditoriaEventos.Select(a => a.Resultado).SingleAsync());
        Assert.False(await _service.RevogarSessaoAsync(sessaoId, usuario.Id, "repetição"));
        Assert.Equal(2, await _context.AuditoriaEventos.CountAsync());
        var ultimoEvento = await _context.AuditoriaEventos.OrderBy(a => a.Id).LastAsync();
        Assert.Equal("Negado", ultimoEvento.Resultado);
    }

    [Fact]
    public async Task CapabilityAdministradorGeral_ConcedeRevogaESessaoDoAlvo()
    {
        var ator = new Usuario { Nome = "Técnico", Login = $"tec-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true, SenhaHash = "hash" };
        var alvo = new Usuario { Nome = "Admin escola", Login = $"admin-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = false, Ativo = true, SenhaHash = "hash" };
        _context.AddRange(ator, alvo);
        await _context.SaveChangesAsync();
        var sessao = new SessaoUsuario { Id = Guid.NewGuid(), UsuarioId = alvo.Id, ExpiraEm = DateTime.UtcNow.AddHours(1) };
        _context.SessoesUsuarios.Add(sessao);
        await _context.SaveChangesAsync();

        Assert.False(alvo.AdministradorGeral);
        Assert.True(await _service.AlterarAdministradorGeralAsync(alvo.Id, true, ator.Id, "onboarding", "corr-1"));
        Assert.True(await _context.Usuarios.Where(u => u.Id == alvo.Id).Select(u => u.AdministradorGeral).SingleAsync());
        var sessaoNova = new SessaoUsuario { Id = Guid.NewGuid(), UsuarioId = alvo.Id, ExpiraEm = DateTime.UtcNow.AddHours(1) };
        _context.SessoesUsuarios.Add(sessaoNova);
        await _context.SaveChangesAsync();
        Assert.True(await _service.AlterarAdministradorGeralAsync(alvo.Id, false, ator.Id, "fim da função", "corr-2"));
        Assert.NotNull(await _context.SessoesUsuarios.Where(s => s.Id == sessao.Id).Select(s => s.RevogadaEm).SingleAsync());
        Assert.NotNull(await _context.SessoesUsuarios.Where(s => s.Id == sessaoNova.Id).Select(s => s.RevogadaEm).SingleAsync());
        var eventos = await _context.AuditoriaEventos.OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(2, eventos.Count);
        Assert.Equal("corr-2", eventos[1].CorrelacaoId);
        Assert.Contains("antes=True; depois=False", eventos[1].Motivo);
        Assert.False(await _service.AlterarAdministradorGeralAsync(alvo.Id, false, ator.Id, "repetição", "corr-3"));
        Assert.Equal(3, await _context.AuditoriaEventos.CountAsync());
    }

    [Fact]
    public async Task CapabilityAdministradorGeral_NaoPermiteAutoelevacaoNemAlvoNaoAdministrador()
    {
        var ator = new Usuario { Nome = "Técnico", Login = $"tec-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true, SenhaHash = "hash" };
        var aluno = new Usuario { Nome = "Aluno", Login = $"aluno-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Aluno, Ativo = true, SenhaHash = "hash" };
        _context.AddRange(ator, aluno);
        await _context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AlterarAdministradorGeralAsync(ator.Id, false, ator.Id, "teste"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AlterarAdministradorGeralAsync(aluno.Id, true, ator.Id, "teste"));
    }

    private async Task<Usuario> CriarUsuarioComEscolaAsync(bool vinculoAtivo)
    {
        var usuario = new Usuario { Nome = "Usuário teste", Login = $"{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, Ativo = true, SenhaHash = "hash" };
        var escola = new Escola { Id = 7, Nome = "Escola teste", Codigo = $"ESC-{Guid.NewGuid()}", Ativa = true };
        _context.Usuarios.Add(usuario);
        _context.Escolas.Add(escola);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = usuario.Id, EscolaId = escola.Id, Papel = PapelEscolar.Professor, Ativo = vinculoAtivo });
        await _context.SaveChangesAsync();
        return usuario;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
