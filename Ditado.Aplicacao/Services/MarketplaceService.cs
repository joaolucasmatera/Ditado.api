using System.ComponentModel.DataAnnotations;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class MarketplaceService(DitadoDbContext context)
{
    public async Task<ErroPublicadoResponse> PublicarAsync(PublicarErroRequest request, int autorId,
        CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var agora = DateTime.UtcNow;
        var erro = new ErroCatalogo
        {
            Codigo = $"ERR_{Guid.NewGuid():N}",
            Nome = request.Nome.Trim(), Descricao = request.Descricao.Trim(),
            ExemploEsperado = NormalizarOpcional(request.ExemploEsperado),
            ExemploResposta = NormalizarOpcional(request.ExemploResposta),
            ObservacaoPedagogica = NormalizarOpcional(request.ObservacaoPedagogica),
            SerieAno = NormalizarOpcional(request.SerieAno),
            AutorId = autorId, Ativo = true, DataCriacao = agora, DataAlteracao = agora
        };
        context.ErrosCatalogo.Add(erro);
        await context.SaveChangesAsync(cancellationToken);
        return Mapear(erro);
    }

    // Consulta mínima do recibo; a listagem compartilhada pertence à CAT-01.
    public async Task<ErroPublicadoResponse?> ObterReciboAsync(int id, int? usuarioId = null, TipoUsuario? tipoUsuario = null, CancellationToken cancellationToken = default)
    {
        var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral, cancellationToken);
        var erro = await context.ErrosCatalogo.AsNoTracking().Include(e => e.Publicacoes).ThenInclude(p => p.TurmasAlvo)
            .SingleOrDefaultAsync(e => e.Id == id && e.Ativo && (usuarioId == null || administradorGlobal ||
                e.Publicacoes.Any(p => p.ChaveVigente != null && (p.Nivel == NivelVisibilidade.PublicoGeral ||
                    (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) ||
                    (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))), cancellationToken);
        return erro is null ? null : Mapear(erro);
    }

    public async Task<List<ErroPublicadoResponse>> ListarAsync(int usuarioId, TipoUsuario tipoUsuario, CancellationToken cancellationToken = default)
    {
        var administradorGlobal = tipoUsuario == TipoUsuario.Administrador && await context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral, cancellationToken);
        var erros = await context.ErrosCatalogo.AsNoTracking()
            .Where(e => e.Ativo && (administradorGlobal || e.Publicacoes.Any(p => p.ChaveVigente != null &&
                (p.Nivel == NivelVisibilidade.PublicoGeral ||
                 (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) ||
                 (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))))
            .OrderByDescending(e => e.DataCriacao).ToListAsync(cancellationToken);
        return erros.Select(Mapear).ToList();
    }

    private static string? NormalizarOpcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static ErroPublicadoResponse Mapear(ErroCatalogo erro) => new(
        erro.Id, erro.Codigo, erro.Nome, erro.Descricao, erro.ExemploEsperado, erro.ExemploResposta,
        erro.ObservacaoPedagogica, erro.SerieAno, erro.AutorId, erro.Ativo, erro.DataCriacao, erro.DataAlteracao);
}
