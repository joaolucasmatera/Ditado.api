using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class RespostaSegmentoTipoErro
{
    public int RespostaSegmentoId { get; set; }
    public TipoErro TipoErro { get; set; }
    public int? ErroCatalogoId { get; set; }
    public ErroCatalogo? ErroCatalogo { get; set; }
    public string? CategoriaPedagogicaCodigo { get; set; }
    public string? CategoriaPedagogicaNome { get; set; }
    public string? CategoriaPedagogicaFamilia { get; set; }

    public RespostaSegmento RespostaSegmento { get; set; } = null!;
}
