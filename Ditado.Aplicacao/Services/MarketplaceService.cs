using System.ComponentModel.DataAnnotations;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Dominio.Entidades;
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
    public async Task<ErroPublicadoResponse?> ObterReciboAsync(int id, CancellationToken cancellationToken = default)
    {
        var erro = await context.ErrosCatalogo.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return erro is null ? null : Mapear(erro);
    }

    private static string? NormalizarOpcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static ErroPublicadoResponse Mapear(ErroCatalogo erro) => new(
        erro.Id, erro.Codigo, erro.Nome, erro.Descricao, erro.ExemploEsperado, erro.ExemploResposta,
        erro.ObservacaoPedagogica, erro.SerieAno, erro.AutorId, erro.Ativo, erro.DataCriacao, erro.DataAlteracao);
}
