namespace Ditado.Aplicacao.Llm;

public sealed class LlmOptions
{
    public bool Enabled { get; set; }
    public const string PromptVersion = "per18-v1";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxInputCharacters { get; set; } = 8000;
    public int MaxOutputTokens { get; set; } = 768;
    public bool Simulated { get; set; }
    public List<LlmProviderOptions> Chain { get; set; } = [];

    public static IReadOnlyDictionary<string, (string Url, string Credential)> Endpoints { get; } =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Groq"] = ("https://api.groq.com/openai/v1", "GROQ_API_KEY"),
            ["Cohere"] = ("https://api.cohere.com/v2", "COHERE_API_KEY"),
            ["GonkaBroker"] = ("https://proxy.gonkabroker.com/v1", "GONKA_BROKER_API_KEY"),
            ["OpenRouter"] = ("https://openrouter.ai/api/v1", "OPENROUTER_API_KEY")
        };

    public bool CadeiaCompleta =>
        TimeoutSeconds is >= 1 and <= 60 && MaxInputCharacters is >= 128 and <= 64000 &&
        MaxOutputTokens is >= 16 and <= 8192 &&
        Chain.Count == Endpoints.Count &&
        Chain.Select(p => p.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Endpoints.Count &&
        Chain.All(p => Endpoints.ContainsKey(p.Provider) && p.Model.Length is > 0 and <= 200 &&
            p.Model.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or ':' or '.' or '_' or '-') &&
            (!p.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) ||
                p.Model.IndexOf('/') is > 0 && !p.Model.EndsWith('/')));

    public bool PodeEnfileirar => Enabled && (Simulated || CadeiaCompleta);
}

public sealed class LlmProviderOptions
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
