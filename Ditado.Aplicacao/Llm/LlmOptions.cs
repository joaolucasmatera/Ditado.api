namespace Ditado.Aplicacao.Llm;

public sealed class LlmOptions
{
    public bool Enabled { get; set; }
    public const string PromptVersion = "per18-v1";
    public int TimeoutSeconds { get; set; } = 15;
    public bool Simulated { get; set; }
    public List<LlmProviderOptions> Chain { get; set; } = [];
    public Dictionary<string, LlmProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LlmProviderOptions
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = string.Empty;
}
