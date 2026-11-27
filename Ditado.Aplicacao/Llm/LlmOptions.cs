namespace Ditado.Aplicacao.Llm;

public sealed class LlmOptions
{
    public bool Enabled { get; set; }
    public const string PromptVersion = "per18-v4-contextual";
    public const string RouterPromptVersion = "per69-v6-exemplos";
    public string VersaoAtiva => RouterConfigurado && !Simulated ? RouterPromptVersion : PromptVersion;
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxInputCharacters { get; set; } = 8000;
    public int MaxOutputTokens { get; set; } = 768;
    public bool Simulated { get; set; }
    public string? RouterUrl { get; set; }
    public bool RouterLogBodies { get; set; }
    public const string RouterProvider = "FreeLlmRouter";
    public const string RouterModel = "auto";
    public const string RouterCredential = "DITADO_LLM_ROUTER_KEY";
    public List<LlmProviderOptions> Chain { get; set; } = [];

    public static IReadOnlyDictionary<string, (string Url, string Credential)> Endpoints { get; } =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Groq"] = ("https://api.groq.com/openai/v1", "GROQ_API_KEY"),
            ["Cohere"] = ("https://api.cohere.com/v2", "COHERE_API_KEY"),
            ["OpenRouter"] = ("https://openrouter.ai/api/v1", "OPENROUTER_API_KEY")
        };

    private bool LimitesValidos =>
        TimeoutSeconds is >= 1 and <= 60 && MaxInputCharacters is >= 128 and <= 64000 &&
        MaxOutputTokens is 0 or (>= 16 and <= 8192);

    public bool RouterConfigurado => LimitesValidos &&
        Uri.TryCreate(RouterUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0 &&
        uri.Fragment.Length == 0 && uri.Query.Length == 0;

    public bool CadeiaCompleta => LimitesValidos && MaxOutputTokens > 0 &&
        Chain.Count == Endpoints.Count &&
        Chain.Select(p => p.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Endpoints.Count &&
        Chain.All(p => Endpoints.ContainsKey(p.Provider) && p.Model.Length is > 0 and <= 200 &&
            p.Model.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or ':' or '.' or '_' or '-') &&
            (!p.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) ||
                p.Model.IndexOf('/') is > 0 && !p.Model.EndsWith('/')));

    public bool PodeEnfileirar => Enabled &&
        (Simulated || RouterConfigurado);
}

public sealed class LlmProviderOptions
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
