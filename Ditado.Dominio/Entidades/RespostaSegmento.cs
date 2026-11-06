using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class RespostaSegmento
{
    public int Id { get; set; }
    public int RespostaDitadoId { get; set; }
    public int SegmentoId { get; set; }
    public string RespostaFornecida { get; set; } = string.Empty;
    public string? RespostaEsperada { get; set; }
    public bool Correto { get; set; }
    public TipoErro? TipoErro { get; set; }
    public MetodoClassificacao? MetodoClassificacao { get; set; }
    public EstadoClassificacao EstadoClassificacao { get; set; } = EstadoClassificacao.NaoClassificado;
    public ICollection<RespostaSegmentoTipoErro> TiposErro { get; set; } = new List<RespostaSegmentoTipoErro>();
    
    public RespostaDitado RespostaDitado { get; set; } = null!;
    public DitadoSegmento Segmento { get; set; } = null!;
}
