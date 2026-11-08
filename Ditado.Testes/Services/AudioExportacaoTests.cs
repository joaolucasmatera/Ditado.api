using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class AudioExportacaoTests : IDisposable
{
    private readonly DitadoDbContext context = new(new DbContextOptionsBuilder<DitadoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Audio_DuasFases_NaoRemoveReferenciado()
    {
        var admin = new Usuario { Nome = "Global", Login = $"g-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true, SenhaHash = "h" };
        var livre = new Ditado.Dominio.Entidades.Ditado { Titulo = "Livre", Autor = admin, AudioLeitura = new byte[] { 1, 2 }, DataCriacao = DateTime.UtcNow.AddHours(-25) };
        var usado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Usado", Autor = admin, AudioLeitura = new byte[] { 3 }, DataCriacao = DateTime.UtcNow.AddHours(-25) };
        context.AddRange(admin, livre, usado);
        await context.SaveChangesAsync();
        context.AplicacoesDitados.Add(new AplicacaoDitado { DitadoId = usado.Id, EscolaId = 1, TurmaId = 1 });
        await context.SaveChangesAsync();
        var service = new AudioRetencaoService(context);
        var candidatos = await service.ListarCandidatosAsync(admin.Id, DateTime.UtcNow);
        Assert.Single(candidatos);
        Assert.Equal(livre.Id, candidatos[0].DitadoId);
        Assert.Equal(1, await service.ConfirmarLimpezaAsync(new[] { livre.Id, usado.Id }, admin.Id, DateTime.UtcNow));
        Assert.Empty((await context.Ditados.SingleAsync(d => d.Id == livre.Id)).AudioLeitura);
        Assert.NotEmpty((await context.Ditados.SingleAsync(d => d.Id == usado.Id)).AudioLeitura);
    }

    [Fact]
    public async Task Exportacao_NaoIncluiAudio_RespeitaLimiteECursor()
    {
        var admin = new Usuario { Nome = "Global", Login = $"g-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true, SenhaHash = "h" };
        var aluno = new Usuario { Nome = "Aluno", Login = $"a-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Aluno, Ativo = true, SenhaHash = "h" };
        var escola = new Escola { Nome = "E", Codigo = $"E-{Guid.NewGuid()}", Ativa = true };
        context.AddRange(admin, aluno, escola);
        await context.SaveChangesAsync();
        var turma = new Turma { Nome = "T", Serie = 1, AnoLetivo = 2026, EscolaId = escola.Id, ProfessorResponsavelId = admin.Id };
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "D", Autor = admin, AudioLeitura = new byte[] { 4 } };
        context.AddRange(turma, ditado);
        await context.SaveChangesAsync();
        var aplicacao = new AplicacaoDitado { DitadoId = ditado.Id, EscolaId = escola.Id, TurmaId = turma.Id };
        context.AplicacoesDitados.Add(aplicacao);
        await context.SaveChangesAsync();
        context.RespostaDitados.Add(new RespostaDitado { DitadoId = ditado.Id, AlunoId = aluno.Id, AplicacaoDitadoId = aplicacao.Id, EscolaId = escola.Id, Nota = 90, DataRealizacao = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var resultado = await new ExportacaoService(context).ExportarRespostasCsvAsync(admin.Id, new[] { escola.Id }, new ExportacaoOpcoes { LimiteLinhas = 1 });
        var csv = System.Text.Encoding.UTF8.GetString(resultado.Conteudo);
        Assert.Contains("escolaId", csv);
        Assert.DoesNotContain("AudioLeitura", csv);
        Assert.Equal(1, resultado.Linhas);
        Assert.Single(await context.AuditoriaEventos.Where(a => a.Operacao == "ExportarRespostas").ToListAsync());
    }

    [Fact]
    public async Task Exportacao_CancelamentoInterrompeSemArquivoCompleto()
    {
        using var cancelamento = new CancellationTokenSource();
        cancelamento.Cancel();
        var admin = new Usuario { Nome = "Global", Login = $"g-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Administrador, AdministradorGeral = true, Ativo = true, SenhaHash = "h" };
        var escola = new Escola { Nome = "E", Codigo = $"E-{Guid.NewGuid()}", Ativa = true };
        context.AddRange(admin, escola);
        await context.SaveChangesAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExportacaoService(context).ExportarRespostasCsvAsync(admin.Id, new[] { escola.Id }, new ExportacaoOpcoes(), cancelamento.Token));
        Assert.Contains("Cancelado", await context.AuditoriaEventos.Select(a => a.Resultado).ToListAsync());
    }

    [Fact]
    public async Task Exportacao_IsolaEscolaSemVinculo()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Professor, Ativo = true, SenhaHash = "h" };
        var escolaA = new Escola { Nome = "A", Codigo = $"A-{Guid.NewGuid()}", Ativa = true };
        var escolaB = new Escola { Nome = "B", Codigo = $"B-{Guid.NewGuid()}", Ativa = true };
        context.AddRange(professor, escolaA, escolaB);
        await context.SaveChangesAsync();
        context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professor.Id, EscolaId = escolaA.Id, Papel = PapelEscolar.Professor, Ativo = true });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ExportacaoService(context).ExportarRespostasCsvAsync(professor.Id, new[] { escolaA.Id, escolaB.Id }, new ExportacaoOpcoes()));
        Assert.Contains("Negado", await context.AuditoriaEventos.Select(a => a.Resultado).ToListAsync());
    }

    [Fact]
    public async Task Exportacao_ProfessorComFlagInconsistente_NaoViraGlobal()
    {
        var professor = new Usuario { Nome = "Professor", Login = $"p-{Guid.NewGuid()}@t", Tipo = TipoUsuario.Professor, AdministradorGeral = true, Ativo = true, SenhaHash = "h" };
        var escola = new Escola { Nome = "E", Codigo = $"E-{Guid.NewGuid()}", Ativa = true };
        context.AddRange(professor, escola);
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ExportacaoService(context).ExportarRespostasCsvAsync(professor.Id, new[] { escola.Id }, new ExportacaoOpcoes()));
    }

    public void Dispose() { context.Database.EnsureDeleted(); context.Dispose(); }
}
