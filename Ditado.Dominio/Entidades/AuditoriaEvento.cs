namespace Ditado.Dominio.Entidades;

public class AuditoriaEvento
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public int? EscolaId { get; set; }
    public string Operacao { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string? CorrelacaoId { get; set; }
    public DateTime DataUtc { get; set; } = DateTime.UtcNow;
}
