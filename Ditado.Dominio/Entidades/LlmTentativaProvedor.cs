namespace Ditado.Dominio.Entidades;

public sealed class LlmTentativaProvedor
{
    public int LlmAnaliseTarefaId { get; set; }
    public int Numero { get; set; }
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string PromptVersao { get; set; } = string.Empty;
    public string Desfecho { get; set; } = "Iniciada";
    public string? Motivo { get; set; }
    public int? HttpStatus { get; set; }
    public DateTime IniciadaEm { get; set; }
    public DateTime? EncerradaEm { get; set; }
}
