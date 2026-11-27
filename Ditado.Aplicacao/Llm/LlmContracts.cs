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
    public LlmRouterPrompt? Router { get; init; }
    [JsonIgnore]
    public LlmCallCorrelation? Correlacao { get; init; }
    [JsonIgnore]
    public IReadOnlyDictionary<int, int>? RespostasInternas { get; init; }
    [JsonIgnore]
    public IEnumerable<LlmGapPrompt> Lacunas => Blocos.SelectMany(bloco => bloco.Lacunas);
}

public sealed record LlmCallCorrelation(int RespostaDitadoId, int LlmAnaliseTarefaId, int NumeroTentativa);

public sealed record LlmRouterPrompt(LlmRouterDitado Ditado, IReadOnlyList<LlmRouterCandidato> ErrosCandidatos,
    IReadOnlyList<LlmRouterResposta> Respostas);
public sealed record LlmRouterDitado(string Titulo, string? Descricao, string TextoComMarcacoes,
    IReadOnlyList<string> Categorias, IReadOnlyList<LlmRouterLacuna> Lacunas);
public sealed record LlmRouterLacuna(int SegmentoId, string Esperado);
public sealed record LlmRouterResposta(int SegmentoId, string? Resposta);
public sealed record LlmRouterCandidato(int Id, string Nome, string Descricao,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExemploEsperado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExemploResposta,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ObservacaoPedagogica,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[][]? Exemplos = null);

public sealed class LlmRouterOutput
{
    [JsonRequired]
    public List<LlmRouterSegmento> Segmentos { get; init; } = [];

    public LlmAnalysisOutput? Converter(LlmAttemptPrompt entrada)
    {
        if (entrada.Router is null || entrada.RespostasInternas is null || Segmentos is null ||
            Segmentos.Count != entrada.RespostasInternas.Count) return null;
        var pendentes = entrada.Lacunas.Where(l => l.Avaliar).Select(l => l.Id).ToHashSet();
        var grupos = entrada.Candidatos.ToDictionary(c => c.Id, c => new LlmClassificationOutput { CandidatoId = c.Id });
        var vistos = new HashSet<int>();
        var saida = new LlmAnalysisOutput { Classificacoes = grupos.Values.ToList() };
        foreach (var segmento in Segmentos)
        {
            if (segmento is null || !vistos.Add(segmento.SegmentoId) ||
                !entrada.RespostasInternas.TryGetValue(segmento.SegmentoId, out var interno) ||
                segmento.CandidatosCorrespondentes is null) return null;
            var correspondentes = segmento.CandidatosCorrespondentes.ToHashSet();
            if (correspondentes.Count != segmento.CandidatosCorrespondentes.Count ||
                !correspondentes.All(grupos.ContainsKey)) return null;
            if (!pendentes.Contains(interno)) continue;
            saida.LacunasAvaliadas.Add(interno);
            if (segmento.Inconclusivo) saida.SegmentosInconclusivos.Add(interno);
            foreach (var (id, grupo) in grupos)
            {
                if (correspondentes.Contains(id)) grupo.Correspondentes.Add(new() { LacunaId = interno });
                else if (segmento.Inconclusivo) grupo.Inconclusivas.Add(new() { LacunaId = interno });
            }
        }
        return saida;
    }
}

public sealed class LlmRouterSegmento
{
    [JsonRequired]
    public int SegmentoId { get; init; }
    [JsonRequired]
    public List<int> CandidatosCorrespondentes { get; init; } = [];
    public bool Inconclusivo { get; init; }
}

public interface ILlmProviderClient
{
    Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, LlmAttemptPrompt entrada, CancellationToken cancellationToken);
}

public sealed class LlmAnalysisOutput
{
    [JsonIgnore]
    public HashSet<int> SegmentosInconclusivos { get; } = [];
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
    int? HttpStatus = null,
    string? ProvedorEfetivo = null,
    string? ModeloEfetivo = null,
    int? TentativasRouter = null);
