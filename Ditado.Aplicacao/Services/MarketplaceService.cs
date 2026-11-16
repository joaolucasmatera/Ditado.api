using System.ComponentModel.DataAnnotations;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class MarketplaceService(DitadoDbContext context, VisibilidadeService visibilidade)
{
    public async Task<ErroPublicadoResponse> PublicarAsync(PublicarErroRequest request, int autorId,
        CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

        var ator = await context.Usuarios.SingleOrDefaultAsync(u => u.Id == autorId && u.Ativo, cancellationToken);
        if (ator is null || (ator.Tipo != TipoUsuario.Professor && ator.Tipo != TipoUsuario.Administrador))
            throw new UnauthorizedAccessException("Ator não autorizado.");

        var escolasAtivas = await context.VinculosEscolares
            .Where(v => v.UsuarioId == autorId && v.Ativo && v.Escola.Ativa)
            .Select(v => v.EscolaId)
            .ToListAsync(cancellationToken);
        var administradorGeral = ator.Tipo == TipoUsuario.Administrador && ator.AdministradorGeral;
        if (!administradorGeral && escolasAtivas.Count == 0)
            throw new UnauthorizedAccessException("Ator sem vínculo escolar ativo.");

        var escolaId = request.EscolaId;
        if (request.Nivel is NivelVisibilidade.Instituicao or NivelVisibilidade.Turmas)
        {
            escolaId ??= escolasAtivas.Select(id => (int?)id).FirstOrDefault();
            if (escolaId is int escolaAlvo && !administradorGeral && !escolasAtivas.Contains(escolaAlvo))
                throw new UnauthorizedAccessException("Ator sem vínculo ativo com a escola.");
        }

        var publicacao = await visibilidade.PrepararPublicacaoErroAsync(
            request.Nivel, escolaId, request.TurmaIds, autorId, versao: 1, cancellationToken);
        var agora = DateTime.UtcNow;
        publicacao.VigenteDesde = agora;
        var erro = new ErroCatalogo
        {
            Codigo = $"ERR_{Guid.NewGuid():N}",
            Nome = request.Nome.Trim(), Descricao = request.Descricao.Trim(),
            ExemploEsperado = NormalizarOpcional(request.ExemploEsperado),
            ExemploResposta = NormalizarOpcional(request.ExemploResposta),
            ObservacaoPedagogica = NormalizarOpcional(request.ObservacaoPedagogica),
            SerieAno = NormalizarOpcional(request.SerieAno),
            AutorId = autorId, Autor = ator, Ativo = true, DataCriacao = agora, DataAlteracao = agora
        };
        erro.Publicacoes.Add(publicacao);
        context.ErrosCatalogo.Add(erro);
        await context.SaveChangesAsync(cancellationToken);
        return Mapear(erro);
    }

    public async Task<ErroPublicadoResponse?> AtualizarAsync(PublicarErroRequest request, int erroId, int atorId,
        TipoUsuario tipoAtor, CancellationToken cancellationToken = default)
    {
        var erro = await context.ErrosCatalogo
            .Include(e => e.Autor)
            .Include(e => e.Publicacoes)
            .SingleOrDefaultAsync(e => e.Id == erroId, cancellationToken);
        if (erro is null)
            return null;
        if (!await visibilidade.PodeAdministrarErroAsync(erro, atorId, cancellationToken, tipoAtor))
            throw new UnauthorizedAccessException("Ator sem permissão para editar este erro no escopo atual.");
        if (!erro.Ativo || erro.ArquivadoEm.HasValue)
            throw new InvalidOperationException("Erros arquivados não podem ser editados.");
        if (await context.AplicacoesErrosCatalogo.AnyAsync(a => a.ErroCatalogoId == erroId, cancellationToken))
            throw new InvalidOperationException("Este erro já foi utilizado em um ditado e não pode mais ser editado.");

        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        erro.Nome = request.Nome.Trim();
        erro.Descricao = request.Descricao.Trim();
        erro.ExemploEsperado = NormalizarOpcional(request.ExemploEsperado);
        erro.ExemploResposta = NormalizarOpcional(request.ExemploResposta);
        erro.ObservacaoPedagogica = NormalizarOpcional(request.ObservacaoPedagogica);
        erro.SerieAno = NormalizarOpcional(request.SerieAno);
        erro.DataAlteracao = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return Mapear(erro);
    }

    // Consulta mínima do recibo; a listagem compartilhada pertence à CAT-01.
    public async Task<ErroPublicadoResponse?> ObterReciboAsync(int id, int? usuarioId = null, TipoUsuario? tipoUsuario = null, CancellationToken cancellationToken = default)
    {
        var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral, cancellationToken);
        var erro = await context.ErrosCatalogo.AsNoTracking().Include(e => e.Autor).Include(e => e.Publicacoes).ThenInclude(p => p.TurmasAlvo)
            .SingleOrDefaultAsync(e => e.Id == id && e.Ativo && (usuarioId == null || administradorGlobal || e.AutorId == usuarioId ||
                e.Publicacoes.Any(p => p.ChaveVigente != null && (p.Nivel == NivelVisibilidade.PublicoGeral ||
                    (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) ||
                    (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))), cancellationToken);
        return erro is null ? null : Mapear(erro);
    }

    public Task<List<ErroPublicadoResponse>> ListarAsync(int usuarioId, TipoUsuario tipoUsuario, CancellationToken cancellationToken = default)
        => ListarAsync(usuarioId, tipoUsuario, "ativo", cancellationToken);

    public async Task<List<ErroPublicadoResponse>> ListarAsync(int usuarioId, TipoUsuario tipoUsuario, string estado, CancellationToken cancellationToken = default)
    {
        var administradorGlobal = tipoUsuario == TipoUsuario.Administrador && await context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral, cancellationToken);
        var arquivado = string.Equals(estado, "arquivado", StringComparison.OrdinalIgnoreCase);
        var escolasDoAtor = context.VinculosEscolares
            .Where(v => v.UsuarioId == usuarioId && v.Ativo && v.Escola.Ativa)
            .Select(v => v.EscolaId);
        var erros = await context.ErrosCatalogo.AsNoTracking()
            .Include(e => e.Autor)
            .Include(e => e.Publicacoes.Where(p => p.ChaveVigente != null))
                .ThenInclude(p => p.TurmasAlvo)
            .Where(e => arquivado ? !e.Ativo : e.Ativo)
            .Where(e => administradorGlobal || e.AutorId == usuarioId ||
                (tipoUsuario == TipoUsuario.Administrador && context.VinculosEscolares.Any(v => v.UsuarioId == e.AutorId && v.Ativo && v.Escola.Ativa && escolasDoAtor.Contains(v.EscolaId))) ||
                (!arquivado && e.Publicacoes.Any(p => p.ChaveVigente != null &&
                    (p.Nivel == NivelVisibilidade.PublicoGeral ||
                     (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && escolasDoAtor.Contains(p.EscolaId.Value)) ||
                     (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))))
            .OrderByDescending(e => e.DataCriacao).ToListAsync(cancellationToken);
        return erros.Select(Mapear).ToList();
    }

    private static string? NormalizarOpcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static ErroPublicadoResponse Mapear(ErroCatalogo erro) => new(
        erro.Id, erro.Codigo, erro.Nome, erro.Descricao, erro.ExemploEsperado, erro.ExemploResposta,
        erro.ObservacaoPedagogica, erro.SerieAno, erro.AutorId, erro.Ativo, erro.DataCriacao, erro.DataAlteracao,
        erro.Autor?.Nome,
        erro.Publicacoes.Where(p => p.ChaveVigente != null).OrderByDescending(p => p.Versao).Select(p => p.Nivel.ToString()).FirstOrDefault() ?? NivelVisibilidade.Instituicao.ToString());
}
