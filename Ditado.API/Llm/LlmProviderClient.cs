using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Ditado.Aplicacao.Llm;

public sealed class LlmProviderClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> options) : ILlmProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<LlmProviderResponse> ClassificarAsync(
        LlmProviderOptions provider,
        string respostaEsperada,
        string respostaFornecida,
        IReadOnlyList<LlmCandidatePrompt> candidatos,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.Model) ||
            !Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps || endpoint.UserInfo.Length != 0 ||
            endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
            return new(LlmProviderOutcome.Tecnica, Error: "Provedor sem modelo ou URL configurados.");

        var apiKey = string.IsNullOrWhiteSpace(provider.ApiKeyEnvironmentVariable)
            ? null
            : Environment.GetEnvironmentVariable(provider.ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new(LlmProviderOutcome.Tecnica, Error: "Chave do provedor não configurada no ambiente.");

        var payload = new
        {
            model = provider.Model,
            messages = new[]
            {
                new { role = "system", content = "Classifique somente entre os candidatos fornecidos. Responda exclusivamente no JSON solicitado. Não crie candidatos." },
                new { role = "user", content = JsonSerializer.Serialize(new { respostaEsperada, respostaFornecida, candidatos }, JsonOptions) }
            },
            response_format = BuildResponseFormat()
        };

        var isCohere = provider.Provider.Equals("Cohere", StringComparison.OrdinalIgnoreCase);
        var path = isCohere ? "/chat" : "/chat/completions";
        var uri = new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}{path}");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create<object>(isCohere ? BuildCoherePayload(provider.Model, respostaEsperada, respostaFornecida, candidatos) : payload, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.Value.TimeoutSeconds, 1, 60)));
            using var response = await httpClientFactory.CreateClient("Llm").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if ((int)response.StatusCode == 408 || (int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                return new(LlmProviderOutcome.Tecnica, Error: $"HTTP {(int)response.StatusCode} do provedor.");
            if (!response.IsSuccessStatusCode)
                return new(LlmProviderOutcome.Permanente, Error: $"HTTP {(int)response.StatusCode} do provedor.");
            var buffer = new byte[100_001];
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var length = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, timeout.Token);
            if (length == buffer.Length)
                return new(LlmProviderOutcome.SaidaInvalida, Error: "Resposta do provedor excedeu o limite.");
            var content = System.Text.Encoding.UTF8.GetString(buffer, 0, length);

            var text = isCohere ? ExtractCohereText(content) : ExtractOpenAiText(content);
            if (string.IsNullOrWhiteSpace(text))
                return new(LlmProviderOutcome.SaidaInvalida, Error: "Resposta sem conteúdo estruturado.");
            var output = JsonSerializer.Deserialize<LlmAnalysisOutput>(text, JsonOptions);
            return output is null
                ? new(LlmProviderOutcome.SaidaInvalida, Error: "JSON vazio do provedor.")
                : new(LlmProviderOutcome.Sucesso, output);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            return new(LlmProviderOutcome.Tecnica, Error: "Falha de rede no provedor.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(LlmProviderOutcome.Tecnica, Error: "Timeout do provedor.");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            return new(LlmProviderOutcome.SaidaInvalida, Error: "JSON inválido do provedor.");
        }
    }

    private static object BuildResponseFormat() => new
    {
        type = "json_schema",
        json_schema = new
        {
            name = "classificacao_ditado",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "classificacoes" },
                properties = new
                {
                    classificacoes = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "candidatoId", "estado", "confianca", "evidencia" },
                            properties = new
                            {
                                candidatoId = new { type = "integer" },
                                estado = new { type = "string", @enum = new[] { "correspondente", "naoCorrespondente", "inconclusivo" } },
                                confianca = new { type = new[] { "number", "null" } },
                                evidencia = new { type = new[] { "string", "null" } }
                            }
                        }
                    }
                }
            }
        }
    };

    private static object BuildCoherePayload(string model, string expected, string provided, IReadOnlyList<LlmCandidatePrompt> candidates) => new
    {
        model,
        messages = new[]
        {
            new { role = "system", content = "Classifique somente entre os candidatos fornecidos. Responda exclusivamente no JSON solicitado. Não crie candidatos." },
            new { role = "user", content = JsonSerializer.Serialize(new { respostaEsperada = expected, respostaFornecida = provided, candidatos = candidates }, JsonOptions) }
        },
        response_format = new { type = "json_object", schema = BuildCohereSchema() }
    };

    private static object BuildCohereSchema() => new
    {
        type = "object",
        required = new[] { "classificacoes" },
        properties = new { classificacoes = new { type = "array" } }
    };

    private static string ExtractOpenAiText(string content)
    {
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private static string ExtractCohereText(string content)
    {
        using var json = JsonDocument.Parse(content);
        var parts = json.RootElement.GetProperty("message").GetProperty("content");
        return parts.EnumerateArray().FirstOrDefault(x => x.TryGetProperty("text", out _)).GetProperty("text").GetString() ?? string.Empty;
    }
}
