namespace Ditado.Dominio.Entidades;

public class AplicacaoErroCatalogo
{
    public int AplicacaoDitadoId { get; set; }
    public int ErroCatalogoId { get; set; }
    public string CodigoSnapshot { get; set; } = string.Empty;
    public string NomeSnapshot { get; set; } = string.Empty;

    public AplicacaoDitado AplicacaoDitado { get; set; } = null!;
    public ErroCatalogo ErroCatalogo { get; set; } = null!;
}
