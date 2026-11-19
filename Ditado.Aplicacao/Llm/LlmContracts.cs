using System.Text.Json.Serialization;

namespace Ditado.Aplicacao.Llm;

public enum LlmProviderOutcome
{
    Sucesso,
    Tecnica,
    Permanente,
    SaidaInvalida,
    Autenticacao
}

public enum LlmMotivo
{
    Configuracao,
    Credencial,
    LimiteProvedor,
    Timeout,
    Rede,
    Indisponivel,
    Contrato,
    LimiteEntrada,
    SaidaInvalida,
    SaidaTruncada,
    Interrupcao
}

public sealed record LlmCandidatePrompt(int Id, string Codigo, string Nome, string Descricao);

public sealed record LlmGapPrompt(int Id, string Esperado, string? Escrito, bool Avaliar);

public sealed record LlmBlocoPrompt(string TextoEscrito, IReadOnlyList<LlmGapPrompt> Lacunas);

public sealed record LlmAttemptPrompt(IReadOnlyList<LlmBlocoPrompt> Blocos, IReadOnlyList<LlmCandidatePrompt> Candidatos)
{
    [JsonIgnore]
    public IEnumerable<LlmGapPrompt> Lacunas => Blocos.SelectMany(bloco => bloco.Lacunas);
}

public interface ILlmProviderClient
{
    Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, LlmAttemptPrompt entrada, CancellationToken cancellationToken);
}

public sealed class LlmAnalysisOutput
{
    [JsonRequired]
    public List<int> LacunasAvaliadas { get; set; } = [];

    [JsonRequired]
    public List<LlmClassificationOutput> Classificacoes { get; set; } = [];
}

public sealed class LlmClassificationOutput
{
    [JsonRequired]
    public int CandidatoId { get; set; }

    [JsonRequired]
    public List<LlmGapFinding> Correspondentes { get; set; } = [];

    [JsonRequired]
    public List<LlmGapFinding> Inconclusivas { get; set; } = [];
}

public sealed class LlmGapFinding
{
    [JsonRequired]
    public int LacunaId { get; set; }

    [JsonRequired]
    public decimal? Confianca { get; set; }

    [JsonRequired]
    public string? Evidencia { get; set; }
}

public sealed record LlmProviderResponse(
    LlmProviderOutcome Outcome,
    LlmAnalysisOutput? Output = null,
    LlmMotivo? Motivo = null,
    int? HttpStatus = null);
