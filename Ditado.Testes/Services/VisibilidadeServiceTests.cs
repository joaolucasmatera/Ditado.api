using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class VisibilidadeServiceTests : IDisposable
{
    private readonly DitadoDbContext _context = new(new DbContextOptionsBuilder<DitadoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly VisibilidadeService _service;

    public VisibilidadeServiceTests() => _service = new VisibilidadeService(_context);

    [Fact]
    public async Task PublicoGeral_NaoAceitaAlvo()
    {
        var ditado = await CriarDitadoAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.PublicoGeral, 1, Array.Empty<int>(), ditado.AutorId!.Value));
    }

    [Fact]
    public async Task Turmas_DeveRejeitarAlvoDeOutraEscola()
    {
        var ditado = await CriarDitadoAsync();
        var escolaA = new Escola { Nome = "A", Codigo = "A", Ativa = true };
        var escolaB = new Escola { Nome = "B", Codigo = "B", Ativa = true };
        var turma = new Turma { Nome = "B1", Serie = 5, AnoLetivo = 2026, Escola = escolaB, ProfessorResponsavel = ditado.Autor! };
        _context.AddRange(escolaA, escolaB, turma);
        await _context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.Turmas, escolaA.Id, new[] { turma.Id }, ditado.AutorId!.Value));
    }

    [Fact]
    public async Task TrocaDeVisibilidade_EncerraAnterior()
    {
        var ditado = await CriarDitadoAsync();
        var escola = new Escola { Id = 1, Nome = "A", Codigo = "A", Ativa = true };
        _context.Escolas.Add(escola);
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = ditado.AutorId!.Value, EscolaId = 1, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();
        var primeira = await _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.PublicoGeral, null, Array.Empty<int>(), ditado.AutorId.Value);
        var segunda = await _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.Instituicao, 1, Array.Empty<int>(), ditado.AutorId.Value);
        Assert.Equal(1, primeira.Versao);
        Assert.Equal(2, segunda.Versao);
        Assert.Single(await _context.PublicacoesDitados.Where(p => p.ChaveVigente == 1).ToListAsync());
        Assert.NotNull(await _context.PublicacoesDitados.Where(p => p.Id == primeira.Id).Select(p => p.EncerradaEm).SingleAsync());
    }

    [Fact]
    public async Task ArquivarERestaurar_NaoRemoveAplicacao()
    {
        var ditado = await CriarDitadoAsync();
        var escola = new Escola { Nome = "A", Codigo = "A", Ativa = true };
        var turma = new Turma { Nome = "A1", Serie = 5, AnoLetivo = 2026, Escola = escola, ProfessorResponsavel = ditado.Autor! };
        _context.AddRange(escola, turma);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = ditado.AutorId!.Value, EscolaId = escola.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();
        _context.AplicacoesDitados.Add(new AplicacaoDitado { DitadoId = ditado.Id, EscolaId = escola.Id, TurmaId = turma.Id });
        await _context.SaveChangesAsync();
        await _service.ArquivarDitadoAsync(ditado.Id, ditado.AutorId!.Value);
        await _service.RestaurarDitadoAsync(ditado.Id, ditado.AutorId!.Value);
        Assert.Single(await _context.AplicacoesDitados.ToListAsync());
    }

    [Fact]
    public async Task AdministradorGeralSemVinculo_PodePublicar()
    {
        var admin = new Usuario { Nome = "Global", Login = $"global-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, SenhaHash = "hash" };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Global", Autor = admin, Ativo = true };
        _context.Add(ditado);
        await _context.SaveChangesAsync();
        var publicacao = await _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.PublicoGeral, null, Array.Empty<int>(), admin.Id);
        Assert.Equal(NivelVisibilidade.PublicoGeral, publicacao.Nivel);
    }

    [Fact]
    public async Task AdministradorSemCapabilityESemVinculo_ENegado()
    {
        var admin = new Usuario { Nome = "Sem escopo", Login = $"local-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Administrador, AdministradorGeral = false, SenhaHash = "hash" };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Negado", Autor = admin, Ativo = true };
        _context.Add(ditado);
        await _context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PublicarDitadoAsync(ditado.Id, NivelVisibilidade.PublicoGeral, null, Array.Empty<int>(), admin.Id));
    }

    private async Task<Ditado.Dominio.Entidades.Ditado> CriarDitadoAsync()
    {
        var autor = new Usuario { Nome = "Autor", Login = $"{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Ditado", Autor = autor, Ativo = true };
        _context.Add(ditado);
        await _context.SaveChangesAsync();
        return ditado;
    }

    public void Dispose() { _context.Database.EnsureDeleted(); _context.Dispose(); }
}
