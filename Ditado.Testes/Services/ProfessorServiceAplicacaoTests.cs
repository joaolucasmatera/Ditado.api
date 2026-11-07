using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class ProfessorServiceAplicacaoTests : IDisposable
{
    private readonly DitadoDbContext _context;
    private readonly ProfessorService _service;

    public ProfessorServiceAplicacaoTests()
    {
        _context = new DitadoDbContext(new DbContextOptionsBuilder<DitadoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _service = new ProfessorService(_context);
    }

    [Fact]
    public async Task ListarMeusDitados_DeveLerAplicacaoNova()
    {
        var (professor, aplicacao) = await CriarAplicacaoAsync();

        var resultado = await _service.ListarMeusDitadosAtribuidosAsync(professor.Id);

        var item = Assert.Single(resultado);
        Assert.Equal(aplicacao.DitadoId, item.DitadoId);
        Assert.Equal(aplicacao.TurmaId, item.TurmaId);
    }

    [Fact]
    public async Task ObterDetalhes_DeveLerAplicacaoNovaEParticipantes()
    {
        var (professor, aplicacao) = await CriarAplicacaoAsync();

        var resultado = await _service.ObterDetalhesAtribuicaoAsync(aplicacao.TurmaId, aplicacao.DitadoId, professor.Id);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado!.TotalAlunos);
        Assert.Single(resultado.Alunos);
    }

    [Fact]
    public async Task ObterDetalhes_NaoDeveMisturarTentativaLegadaDoMesmoDitado()
    {
        var (professor, aplicacao) = await CriarAplicacaoAsync();
        var alunoId = await _context.Usuarios.Where(u => u.Tipo == TipoUsuario.Aluno).Select(u => u.Id).SingleAsync();
        _context.RespostaDitados.AddRange(
            new RespostaDitado { DitadoId = aplicacao.DitadoId, AlunoId = alunoId, Nota = 20, DataRealizacao = DateTime.UtcNow.AddMinutes(-2) },
            new RespostaDitado { DitadoId = aplicacao.DitadoId, AplicacaoDitadoId = aplicacao.Id, EscolaId = aplicacao.EscolaId, AlunoId = alunoId, Nota = 90, DataRealizacao = DateTime.UtcNow.AddMinutes(-1) });
        await _context.SaveChangesAsync();

        var resultado = await _service.ObterDetalhesAtribuicaoAsync(aplicacao.TurmaId, aplicacao.DitadoId, professor.Id);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado!.AlunosQueFizeram);
        Assert.Equal(90, resultado.NotaMedia);
    }

    private async Task<(Usuario Professor, AplicacaoDitado Aplicacao)> CriarAplicacaoAsync()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Professor, SenhaHash = "hash" };
        var aluno = new Usuario { Nome = "Aluno", Login = $"a-{Guid.NewGuid()}@teste", Tipo = TipoUsuario.Aluno, SenhaHash = "hash" };
        var turma = new Turma { Nome = "Turma", Serie = 5, AnoLetivo = 2026, ProfessorResponsavel = professor };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Ditado", Ativo = true, Autor = professor };
        var escola = new Escola { Nome = "Escola", Codigo = Guid.NewGuid().ToString(), Ativa = true };
        _context.AddRange(professor, aluno, turma, ditado, escola);
        await _context.SaveChangesAsync();
        var aplicacao = new AplicacaoDitado { EscolaId = escola.Id, TurmaId = turma.Id, DitadoId = ditado.Id, DataLimite = DateTime.UtcNow.AddDays(1) };
        _context.AplicacoesDitados.Add(aplicacao);
        await _context.SaveChangesAsync();
        _context.ParticipacoesAplicacoes.Add(new ParticipacaoAplicacao { AplicacaoDitadoId = aplicacao.Id, AlunoId = aluno.Id });
        await _context.SaveChangesAsync();
        return (professor, aplicacao);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
