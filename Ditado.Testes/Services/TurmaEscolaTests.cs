using Ditado.Aplicacao.DTOs.Turmas;
using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class TurmaEscolaTests : IDisposable
{
    private readonly DitadoDbContext _context = new(new DbContextOptionsBuilder<DitadoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CriarTurma_SemEscola_DeveFalhar()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        _context.Usuarios.Add(professor);
        await _context.SaveChangesAsync();
        var service = new TurmaService(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarTurmaAsync(new CriarTurmaRequest
        {
            Nome = "Turma nova", Serie = 5, AnoLetivo = 2026, ProfessorResponsavelId = professor.Id
        }, professor.Id, TipoUsuario.Professor));
    }

    [Fact]
    public async Task CriarTurma_ComEscolaAtiva_DevePersistirEscolaId()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var escola = new Escola { Nome = "Escola", Codigo = Guid.NewGuid().ToString(), Ativa = true };
        _context.AddRange(professor, escola, new VinculoEscolar { Usuario = professor, Escola = escola, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();
        var service = new TurmaService(_context);

        await service.CriarTurmaAsync(new CriarTurmaRequest
        {
            Nome = "Turma nova", Serie = 5, AnoLetivo = 2026, ProfessorResponsavelId = professor.Id, EscolaId = escola.Id
        }, professor.Id, TipoUsuario.Professor);

        Assert.Equal(escola.Id, await _context.Turmas.Select(t => t.EscolaId).SingleAsync());
    }

    [Fact]
    public async Task CriarTurma_ProfessorNaoPodeUsarOutroCallerOuOutraEscola()
    {
        var professorResponsavel = new Usuario { Nome = "Responsável", Login = $"r-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var caller = new Usuario { Nome = "Caller", Login = $"c-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var escola = new Escola { Nome = "Escola", Codigo = Guid.NewGuid().ToString(), Ativa = true };
        _context.AddRange(professorResponsavel, caller, escola, new VinculoEscolar { Usuario = caller, Escola = escola, Papel = PapelEscolar.Professor, Ativo = true });
        await _context.SaveChangesAsync();
        var service = new TurmaService(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarTurmaAsync(new CriarTurmaRequest { Nome = "Turma", Serie = 5, AnoLetivo = 2026, ProfessorResponsavelId = professorResponsavel.Id, EscolaId = escola.Id }, caller.Id, TipoUsuario.Professor));
    }

    [Fact]
    public async Task AtribuirDitado_TurmaLegadaSemEscola_DeveBloquearNovaAtribuicao()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var turma = new Turma { Nome = "Legada", Serie = 5, AnoLetivo = 2026, ProfessorResponsavel = professor };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Ditado", Ativo = true, Autor = professor };
        _context.AddRange(professor, turma, ditado);
        await _context.SaveChangesAsync();
        var service = new TurmaService(_context);

        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AtribuirDitadoAsync(turma.Id, new AtribuirDitadoRequest { DitadoId = ditado.Id, DataLimite = DateTime.UtcNow.AddDays(1) }));

        Assert.Contains("sem EscolaId", erro.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
