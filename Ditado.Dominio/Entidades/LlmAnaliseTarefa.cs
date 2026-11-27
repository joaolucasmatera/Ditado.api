using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class LlmAnaliseTarefa
{
    public int Id { get; set; }
    public int RespostaSegmentoId { get; set; }
    public LlmAnaliseEstado Estado { get; set; } = LlmAnaliseEstado.Pendente;
    public int Tentativas { get; set; }
    public string PromptVersao { get; set; } = string.Empty;
    public string? Provedor { get; set; }
    public string? Modelo { get; set; }
    public string? Origem { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? IniciadaEm { get; set; }
    public DateTime? ConcluidaEm { get; set; }

    public RespostaSegmento RespostaSegmento { get; set; } = null!;
    public ICollection<LlmAnaliseCandidato> Candidatos { get; set; } = new List<LlmAnaliseCandidato>();
    public ICollection<LlmTentativaProvedor> TentativasProvedor { get; set; } = new List<LlmTentativaProvedor>();
}
