using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Ditado.Aplicacao.Llm;

public sealed class LlmProviderClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> options,
    Func<string, string?>? credenciais = null) : ILlmProviderClient
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
        if (!LlmOptions.Endpoints.TryGetValue(provider.Provider, out var endpoint) || string.IsNullOrWhiteSpace(provider.Model))
            return new(LlmProviderOutcome.Permanente, Motivo: LlmMotivo.Configuracao);

        var entrada = JsonSerializer.Serialize(new { respostaEsperada, respostaFornecida, candidatos }, JsonOptions);
        if (entrada.Length > options.Value.MaxInputCharacters)
            return new(LlmProviderOutcome.Permanente, Motivo: LlmMotivo.LimiteEntrada);

        var apiKey = (credenciais ?? Environment.GetEnvironmentVariable)(endpoint.Credential);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Any(char.IsWhiteSpace))
            return new(LlmProviderOutcome.Autenticacao, Motivo: LlmMotivo.Credencial);

        var isCohere = provider.Provider.Equals("Cohere", StringComparison.OrdinalIgnoreCase);
        var isOpenRouter = provider.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase);
        var isGroq = provider.Provider.Equals("Groq", StringComparison.OrdinalIgnoreCase);
        var payload = new Dictionary<string, object>
        {
            ["model"] = provider.Model,
            ["messages"] = new[]
            {
                new { role = "system", content = "Classifique somente entre os candidatos fornecidos. Responda exclusivamente no JSON solicitado. Não crie candidatos." },
                new { role = "user", content = entrada }
            },
            ["response_format"] = isCohere
                ? new { type = "json_object", schema = BuildSchema() }
                : new { type = "json_schema", json_schema = new { name = "classificacao_ditado", strict = true, schema = BuildSchema() } },
            [isGroq ? "max_completion_tokens" : "max_tokens"] = options.Value.MaxOutputTokens
        };
        if (isOpenRouter)
            payload["provider"] = new { require_parameters = true, allow_fallbacks = false };

        var path = isCohere ? "/chat" : "/chat/completions";
        var uri = new Uri(endpoint.Url + path);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        int? statusRecebido = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
            using var client = httpClientFactory.CreateClient("Llm");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            var httpStatus = (int)response.StatusCode;
            statusRecebido = httpStatus;
            if (!response.IsSuccessStatusCode && !(isOpenRouter && httpStatus == 403))
                return HttpError(httpStatus, isOpenRouter);
            var buffer = new byte[100_001];
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var length = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, timeout.Token);
            if (httpStatus == 403)
                return OrcamentoOpenRouter(buffer.AsMemory(0, length))
                    ? new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.LimiteProvedor, HttpStatus: 403)
                    : HttpError(403, isOpenRouter);
            if (length == buffer.Length)
                return new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaInvalida);
            using var json = JsonDocument.Parse(buffer.AsMemory(0, length));
            var root = json.RootElement;
            if (isOpenRouter && root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("code", out var errorCode) && errorCode.ValueKind == JsonValueKind.Number &&
                errorCode.TryGetInt32(out var code))
                return HttpError(code, isOpenRouter);

            var envelope = isCohere ? root : root.GetProperty("choices")[0];
            var finish = envelope.GetProperty("finish_reason").GetString();
            if (finish is "length" or "MAX_TOKENS")
                return new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaTruncada);
            if (finish != (isCohere ? "COMPLETE" : "stop"))
                return new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaInvalida);

            var content = envelope.GetProperty("message").GetProperty("content");
            var text = isCohere
                ? string.Concat(content.EnumerateArray().Select(part => part.GetProperty("text").GetString()))
                : content.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaInvalida);
            var output = JsonSerializer.Deserialize<LlmAnalysisOutput>(text, JsonOptions);
            return output is null
                ? new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaInvalida)
                : new(LlmProviderOutcome.Sucesso, output);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            if (statusRecebido == 403) return HttpError(403, isOpenRouter);
            return new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Rede);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (statusRecebido == 403) return HttpError(403, isOpenRouter);
            return new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Timeout);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            return new(LlmProviderOutcome.SaidaInvalida, Motivo: LlmMotivo.SaidaInvalida);
        }
    }

    private static LlmProviderResponse HttpError(int status, bool isOpenRouter) => status switch
    {
        408 => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Timeout, HttpStatus: status),
        429 => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.LimiteProvedor, HttpStatus: status),
        402 when isOpenRouter => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.LimiteProvedor, HttpStatus: status),
        >= 500 and <= 599 => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Indisponivel, HttpStatus: status),
        401 or 403 or 498 => new(LlmProviderOutcome.Autenticacao, Motivo: LlmMotivo.Credencial, HttpStatus: status),
        _ => new(LlmProviderOutcome.Permanente, Motivo: LlmMotivo.Contrato, HttpStatus: status)
    };

    private static bool OrcamentoOpenRouter(ReadOnlyMemory<byte> body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("error").GetProperty("metadata").GetProperty("limit_source").GetString()
                is "openrouter_guardrail_budget" or "openrouter_workspace_budget" or "openrouter_key_limit";
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return false;
        }
    }

    private static object BuildSchema() => new
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
                                confianca = new { anyOf = new[] { new { type = "number" }, new { type = "null" } } },
                                evidencia = new { anyOf = new[] { new { type = "string" }, new { type = "null" } } }
                            }
                        }
                    }
                }
            };
}
