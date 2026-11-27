namespace Ditado.Dominio.Entidades;

public class DitadoErroCatalogo
{
    public int DitadoId { get; set; }
    public int ErroCatalogoId { get; set; }
    public Ditado Ditado { get; set; } = null!;
    public ErroCatalogo ErroCatalogo { get; set; } = null!;
}
