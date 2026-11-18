using System.Text.Json.Serialization;

namespace Ditado.Aplicacao.Llm;

public enum LlmProviderOutcome
{
    Sucesso,
    Tecnica,
    Permanente,
    SaidaInvalida
}

public sealed record LlmCandidatePrompt(int Id, string Codigo, string Nome, string Descricao);

public interface ILlmProviderClient
{
    Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, string respostaEsperada,
        string respostaFornecida, IReadOnlyList<LlmCandidatePrompt> candidatos, CancellationToken cancellationToken);
}

public sealed class LlmAnalysisOutput
{
    [JsonPropertyName("classificacoes")]
    [JsonRequired]
    public List<LlmClassificationOutput> Classificacoes { get; set; } = [];
}

public sealed class LlmClassificationOutput
{
    [JsonPropertyName("candidatoId")]
    [JsonRequired]
    public int CandidatoId { get; set; }

    [JsonPropertyName("estado")]
    [JsonRequired]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("confianca")]
    [JsonRequired]
    public decimal? Confianca { get; set; }

    [JsonPropertyName("evidencia")]
    [JsonRequired]
    public string? Evidencia { get; set; }
}

public sealed record LlmProviderResponse(
    LlmProviderOutcome Outcome,
    LlmAnalysisOutput? Output = null,
    string? Error = null);
