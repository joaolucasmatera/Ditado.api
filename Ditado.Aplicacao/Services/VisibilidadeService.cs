using System.ComponentModel.DataAnnotations;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public sealed class VisibilidadeService(DitadoDbContext context)
{
    // Compatibilidade apenas para testes legados; endpoints usam sempre o ator autenticado.
    internal Task RestaurarDitadoAsync(int ditadoId) => RestaurarDitadoAsync(ditadoId, 0);
    internal Task RestaurarErroCatalogoAsync(int erroId) => RestaurarErroCatalogoAsync(erroId, 0);
    public async Task<PublicacaoDitado> PublicarDitadoAsync(int ditadoId, NivelVisibilidade nivel, int? escolaId, IReadOnlyCollection<int> turmaIds, int atorId)
    {
        await ValidarAtorAsync(atorId, escolaId, nivel);
        var ditado = await context.Ditados.SingleOrDefaultAsync(d => d.Id == ditadoId);
        if (ditado is null || !ditado.Ativo || ditado.ArquivadoEm.HasValue)
            throw new InvalidOperationException("Ditado inexistente ou arquivado.");
        var turmas = await context.Turmas.Where(t => turmaIds.Contains(t.Id)).ToListAsync();
        ValidarEscopo(nivel, escolaId, turmaIds, turmas);

        var transacao = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync() : null;
        try
        {
            var vigente = await context.PublicacoesDitados.SingleOrDefaultAsync(p => p.DitadoId == ditadoId && p.ChaveVigente == 1);
            var versao = (await context.PublicacoesDitados.Where(p => p.DitadoId == ditadoId).MaxAsync(p => (int?)p.Versao) ?? 0) + 1;
            if (vigente is not null)
            {
                vigente.ChaveVigente = null;
                vigente.EncerradaEm = DateTime.UtcNow;
                vigente.EncerradaPorId = atorId;
            }
            var publicacao = new PublicacaoDitado { DitadoId = ditadoId, Nivel = nivel, EscolaId = escolaId, Versao = versao, ChaveVigente = 1, CriadaPorId = atorId };
            foreach (var turma in turmas) publicacao.TurmasAlvo.Add(new PublicacaoDitadoTurma { TurmaId = turma.Id });
            context.PublicacoesDitados.Add(publicacao);
            await context.SaveChangesAsync();
            if (transacao is not null) await transacao.CommitAsync();
            return publicacao;
        }
        catch (DbUpdateException ex)
        {
            if (transacao is not null) await transacao.RollbackAsync();
            throw new InvalidOperationException("Conflito de concorrência ao publicar o ditado; tente novamente.", ex);
        }
        finally
        {
            if (transacao is not null) await transacao.DisposeAsync();
        }
    }

    internal async Task<PublicacaoErroCatalogo> PrepararPublicacaoErroAsync(
        NivelVisibilidade nivel, int? escolaId, IReadOnlyCollection<int>? turmaIds, int atorId, int versao,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(nivel))
            throw new ValidationException("Nível de visibilidade inválido.");
        if (turmaIds is null)
            throw new ValidationException("Informe as turmas alvo como uma lista.");

        var turmas = await context.Turmas.Where(t => turmaIds.Contains(t.Id)).ToListAsync(cancellationToken);
        if (ObterErroEscopo(nivel, escolaId, turmaIds, turmas) is { } erroEscopo)
            throw new ValidationException(erroEscopo);
        if (escolaId.HasValue && !await context.Escolas.AnyAsync(e => e.Id == escolaId && e.Ativa, cancellationToken))
            throw new ValidationException("A escola da publicação deve existir e estar ativa.");

        return new PublicacaoErroCatalogo
        {
            Nivel = nivel,
            EscolaId = escolaId,
            Versao = versao,
            ChaveVigente = 1,
            CriadaPorId = atorId,
            TurmasAlvo = turmas.Select(t => new PublicacaoErroCatalogoTurma { TurmaId = t.Id }).ToList()
        };
    }

    public async Task<PublicacaoErroCatalogo?> PublicarErroCatalogoAsync(
        int erroId, NivelVisibilidade nivel, int? escolaId, IReadOnlyCollection<int> turmaIds, int atorId,
        CancellationToken cancellationToken)
    {
        await using var transacao = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var erro = await context.ErrosCatalogo.SingleOrDefaultAsync(e => e.Id == erroId, cancellationToken);
            if (erro is null || !await PodeAdministrarErroAsync(erro, atorId, cancellationToken))
                return null;
            if (!erro.Ativo || erro.ArquivadoEm.HasValue)
                throw new InvalidOperationException("Erro de catálogo arquivado.");

            await ValidarAtorAsync(atorId, escolaId, nivel);
            var vigente = await context.PublicacoesErrosCatalogo.SingleOrDefaultAsync(
                p => p.ErroCatalogoId == erroId && p.ChaveVigente == 1, cancellationToken);
            var versao = (await context.PublicacoesErrosCatalogo.Where(p => p.ErroCatalogoId == erroId)
                .MaxAsync(p => (int?)p.Versao, cancellationToken) ?? 0) + 1;
            var publicacao = await PrepararPublicacaoErroAsync(nivel, escolaId, turmaIds, atorId, versao, cancellationToken);
            if (vigente is not null) { vigente.ChaveVigente = null; vigente.EncerradaEm = DateTime.UtcNow; vigente.EncerradaPorId = atorId; }
            publicacao.ErroCatalogoId = erroId;
            context.PublicacoesErrosCatalogo.Add(publicacao);
            await context.SaveChangesAsync(cancellationToken);
            if (transacao is not null) await transacao.CommitAsync(cancellationToken);
            return publicacao;
        }
        catch (DbUpdateException ex)
        {
            if (transacao is not null) await transacao.RollbackAsync();
            throw new InvalidOperationException("Conflito de concorrência ao publicar o erro; tente novamente.", ex);
        }
    }

    public async Task ArquivarDitadoAsync(int ditadoId, int atorId)
    {
        var ditado = await context.Ditados.SingleOrDefaultAsync(d => d.Id == ditadoId) ?? throw new InvalidOperationException("Ditado não encontrado.");
        await ValidarAtorAsync(atorId, await context.PublicacoesDitados.Where(p => p.DitadoId == ditadoId && p.ChaveVigente != null).Select(p => p.EscolaId).FirstOrDefaultAsync(), NivelVisibilidade.Instituicao, ditado.AutorId);
        ditado.Ativo = false; ditado.ArquivadoEm = DateTime.UtcNow; ditado.ArquivadoPorId = atorId;
        await context.SaveChangesAsync();
    }

    public async Task RestaurarDitadoAsync(int ditadoId, int atorId)
    {
        var ditado = await context.Ditados.SingleOrDefaultAsync(d => d.Id == ditadoId) ?? throw new InvalidOperationException("Ditado não encontrado.");
        await ValidarAtorAsync(atorId, await context.PublicacoesDitados.Where(p => p.DitadoId == ditadoId && p.ChaveVigente != null).Select(p => p.EscolaId).FirstOrDefaultAsync(), NivelVisibilidade.Instituicao, ditado.AutorId);
        ditado.Ativo = true; ditado.ArquivadoEm = null; ditado.ArquivadoPorId = null;
        await context.SaveChangesAsync();
    }

    public async Task ArquivarErroCatalogoAsync(int erroId, int atorId)
    {
        await ValidarAtorAsync(atorId, null, NivelVisibilidade.PublicoGeral);
        var erro = await context.ErrosCatalogo.SingleOrDefaultAsync(e => e.Id == erroId) ?? throw new InvalidOperationException("Erro de catálogo não encontrado.");
        await ValidarAtorAsync(atorId, await context.PublicacoesErrosCatalogo.Where(p => p.ErroCatalogoId == erroId && p.ChaveVigente != null).Select(p => p.EscolaId).FirstOrDefaultAsync(), NivelVisibilidade.Instituicao, erro.AutorId);
        erro.Ativo = false; erro.ArquivadoEm = DateTime.UtcNow; erro.ArquivadoPorId = atorId;
        await context.SaveChangesAsync();
    }

    public async Task RestaurarErroCatalogoAsync(int erroId, int atorId)
    {
        var erro = await context.ErrosCatalogo.SingleOrDefaultAsync(e => e.Id == erroId) ?? throw new InvalidOperationException("Erro de catálogo não encontrado.");
        await ValidarAtorAsync(atorId, await context.PublicacoesErrosCatalogo.Where(p => p.ErroCatalogoId == erroId && p.ChaveVigente != null).Select(p => p.EscolaId).FirstOrDefaultAsync(), NivelVisibilidade.Instituicao, erro.AutorId);
        erro.Ativo = true; erro.ArquivadoEm = null; erro.ArquivadoPorId = null;
        await context.SaveChangesAsync();
    }

    private async Task ValidarAtorAsync(int atorId, int? escolaId, NivelVisibilidade nivel, int? autorId = null)
    {
        var ator = await context.Usuarios.SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo);
        if (ator is null || (ator.Tipo != TipoUsuario.Professor && ator.Tipo != TipoUsuario.Administrador))
            throw new InvalidOperationException("Ator não autorizado.");
        if (ator.Tipo == TipoUsuario.Professor && autorId.HasValue && autorId.Value != atorId)
            throw new InvalidOperationException("Professor só pode operar recurso próprio.");
        var vinculoEscola = escolaId.HasValue && await context.VinculosEscolares.AnyAsync(v => v.UsuarioId == atorId && v.EscolaId == escolaId && v.Ativo && v.Escola.Ativa);
        if (escolaId.HasValue && !vinculoEscola && !(ator.Tipo == TipoUsuario.Administrador && ator.AdministradorGeral))
            throw new InvalidOperationException("Ator sem vínculo ativo com a escola.");
        if (nivel == NivelVisibilidade.PublicoGeral && ator.Tipo == TipoUsuario.Professor && !await context.VinculosEscolares.AnyAsync(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa))
            throw new InvalidOperationException("Professor sem vínculo escolar ativo.");
        if (ator.Tipo == TipoUsuario.Administrador && !ator.AdministradorGeral && !await context.VinculosEscolares.AnyAsync(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa))
            throw new InvalidOperationException("Administrador sem vínculo escolar ou capability global não autorizado.");
    }

    internal async Task<bool> PodeAdministrarErroAsync(
        ErroCatalogo erro, int atorId, CancellationToken cancellationToken, TipoUsuario? tipoEsperado = null)
    {
        var ator = await context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo, cancellationToken);
        if (ator is null || (tipoEsperado.HasValue && ator.Tipo != tipoEsperado))
            return false;

        var escolasDoAtor = context.VinculosEscolares
            .Where(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa)
            .Select(v => v.EscolaId);
        if (ator.Tipo == TipoUsuario.Professor)
            return atorId == erro.AutorId && await escolasDoAtor.AnyAsync(cancellationToken);
        if (ator.Tipo != TipoUsuario.Administrador)
            return false;
        if (ator.AdministradorGeral)
            return true;

        return await context.VinculosEscolares.AnyAsync(v => v.UsuarioId == erro.AutorId && v.Ativo && v.Escola.Ativa && escolasDoAtor.Contains(v.EscolaId), cancellationToken)
            || await context.PublicacoesErrosCatalogo.AnyAsync(p => p.ErroCatalogoId == erro.Id && p.ChaveVigente != null && p.EscolaId.HasValue && escolasDoAtor.Contains(p.EscolaId.Value), cancellationToken);
    }

    private static void ValidarEscopo(NivelVisibilidade nivel, int? escolaId, IReadOnlyCollection<int> turmaIds, IReadOnlyCollection<Turma> turmas)
    {
        if (ObterErroEscopo(nivel, escolaId, turmaIds, turmas) is { } erroEscopo)
            throw new InvalidOperationException(erroEscopo);
    }

    private static string? ObterErroEscopo(NivelVisibilidade nivel, int? escolaId, IReadOnlyCollection<int> turmaIds, IReadOnlyCollection<Turma> turmas)
    {
        if (nivel == NivelVisibilidade.PublicoGeral && (escolaId.HasValue || turmaIds.Count > 0))
            return "Publicação pública não aceita escola ou turmas alvo.";
        if (nivel == NivelVisibilidade.Instituicao && (!escolaId.HasValue || turmaIds.Count > 0))
            return "Publicação institucional exige escola e não aceita turmas alvo.";
        if (nivel == NivelVisibilidade.Turmas && (!escolaId.HasValue || turmaIds.Count == 0 || turmas.Count != turmaIds.Distinct().Count()))
            return "Publicação por turmas exige alvos válidos da escola.";
        if (turmas.Any(t => t.EscolaId != escolaId))
            return "Todos os alvos devem pertencer à escola da publicação.";
        return null;
    }
}
