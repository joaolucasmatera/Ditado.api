using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ditado.Aplicacao.Services;

internal static class SelecaoErrosCatalogo
{
    internal static IQueryable<ErroCatalogo> Disponiveis(
        DitadoDbContext context, int? escolaId, int? turmaId = null, int? professorId = null)
        => context.ErrosCatalogo.Where(e => e.Ativo && e.ArquivadoEm == null &&
            e.Publicacoes.Any(p => p.ChaveVigente != null &&
                (p.Nivel == NivelVisibilidade.PublicoGeral ||
                 (escolaId.HasValue && p.EscolaId == escolaId &&
                    (p.Nivel == NivelVisibilidade.Instituicao ||
                     (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t =>
                        t.Turma.Ativo && t.Turma.EscolaId == escolaId &&
                        (!turmaId.HasValue || t.TurmaId == turmaId) &&
                        (!professorId.HasValue || t.Turma.ProfessorResponsavelId == professorId))))))));

    internal static async Task ValidarDestinoAsync(
        DitadoDbContext context, int? escolaId, int turmaId, IReadOnlyCollection<int> ids)
    {
        if (ids.Count != 0 &&
            await Disponiveis(context, escolaId, turmaId).CountAsync(e => ids.Contains(e.Id)) != ids.Count)
            throw new InvalidOperationException("A seleção contém erros indisponíveis para a escola ou turma da atribuição.");
    }

    // A edição, a atribuição e a submissão serializam a leitura da seleção pela linha do ditado.
    internal static async Task<IDbContextTransaction?> BloquearDitadoAsync(DitadoDbContext context, int ditadoId)
    {
        if (!context.Database.IsRelational())
            return null;

        var transacao = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT `Id` FROM `Ditados` WHERE `Id` = {ditadoId} FOR UPDATE");
            return transacao;
        }
        catch
        {
            await transacao.DisposeAsync();
            throw;
        }
    }

    internal static AplicacaoErroCatalogo RegistrarAplicacao(ErroCatalogo erro) => new()
    {
        ErroCatalogoId = erro.Id,
        CodigoSnapshot = erro.Codigo,
        NomeSnapshot = erro.Nome
    };
}
