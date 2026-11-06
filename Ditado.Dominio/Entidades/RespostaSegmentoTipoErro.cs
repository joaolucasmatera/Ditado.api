using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class RespostaSegmentoTipoErro
{
    public int RespostaSegmentoId { get; set; }
    public TipoErro TipoErro { get; set; }

    public RespostaSegmento RespostaSegmento { get; set; } = null!;
}
