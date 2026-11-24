using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class LlmAnaliseCandidato
{
    public int Id { get; set; }
    public int LlmAnaliseTarefaId { get; set; }
    public int ErroCatalogoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? ExemploEsperado { get; set; }
    public string? ExemploResposta { get; set; }
    public string? ObservacaoPedagogica { get; set; }
    public int Versao { get; set; }
    public LlmCandidatoEstado Estado { get; set; } = LlmCandidatoEstado.NaoAvaliado;
    public decimal? Confianca { get; set; }
    public string? Evidencia { get; set; }
    public DateTime? AvaliadoEm { get; set; }

    public LlmAnaliseTarefa LlmAnaliseTarefa { get; set; } = null!;
    public ErroCatalogo ErroCatalogo { get; set; } = null!;
}
