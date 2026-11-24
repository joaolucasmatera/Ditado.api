using System.Buffers;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ditado.Aplicacao.Llm;

public sealed class LlmRouterDiagnostics(ILogger<LlmRouterDiagnostics> logger, IConfiguration configuration)
{
    private const string Oculto = "[omitido por segurança]";
    private static readonly Regex IndicioDeSegredo = new(
        @"\bbearer\s+\S|\bbasic\s+[A-Za-z0-9+/]{8,}={0,2}|\b(?:authorization|api[ _-]?key|password|passwd|senha|secret|token|cookie)""?\s*[:=]\s*\S|-----BEGIN [A-Z ]*PRIVATE KEY|\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+|\b(?:sk-|gsk_|ghp_)[A-Za-z0-9_-]{12,}|://[^\s/@]+@|\b[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}\b|\b\d{3}\.\d{3}\.\d{3}-\d{2}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ChaveSensivel = new(
        @"authorization|password|passwd|senha|secret|(?:api|private|signing|access|router|client|encryption)key|(?:access|refresh|id|auth|session|bearer)token|^(?:key|pwd|token|cookie|setcookie|(?:request|response)?headers|credentials?|credenciais|pin|cpf|email|telefone|phonenumber|matricula)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool Habilitado => logger.IsEnabled(LogLevel.Information);

    public void Registrar(LlmCallCorrelation? correlacao, int? status, double latenciaMs, string? provedor,
        string? modelo, int? tentativas, LlmProviderResponse? resultado, ReadOnlyMemory<byte> carga,
        ReadOnlyMemory<byte>? resposta,
        string chaveRouter)
    {
        var segredos = configuration.AsEnumerable()
            .Where(item => item.Key.Split(':').Any(NomeSensivel))
            .Select(item => item.Value).Append(chaveRouter).Append(SenhaDoBanco())
            .OfType<string>().Where(valor => valor.Length > 0).Distinct(StringComparer.Ordinal).ToArray();

        // shortcut: valores sem identificação em texto livre só podem ser mascarados quando conhecidos ou reconhecíveis.
        bool Sigiloso(string valor) => IndicioDeSegredo.IsMatch(valor) ||
            segredos.Any(segredo => valor.Contains(segredo, StringComparison.Ordinal));
        string? Proteger(string? valor) => valor is null ? null :
            Sigiloso(valor) ? Oculto : valor;

        logger.LogInformation(
            "FreeLLM chamada {RequestId} RespostaDitadoId={RespostaDitadoId} LlmAnaliseTarefaId={LlmAnaliseTarefaId} " +
            "Tentativa={Tentativa} HTTP={StatusHttp} LatenciaMs={LatenciaMs} Provedor={Provedor} Modelo={Modelo} " +
            "x-free-llm-attempts={TentativasRouter} PromptVersao={PromptVersao} Desfecho={Desfecho} Motivo={Motivo} " +
            "Payload={Payload} Resposta={Resposta}",
            Guid.NewGuid(), correlacao?.RespostaDitadoId, correlacao?.LlmAnaliseTarefaId, correlacao?.NumeroTentativa,
            status, (long)latenciaMs, Proteger(provedor), Proteger(modelo), tentativas, LlmOptions.RouterPromptVersion,
            resultado?.Outcome.ToString() ?? "Interrompida", resultado?.Motivo?.ToString() ??
                (resultado is null ? nameof(LlmMotivo.Interrupcao) : null),
            ProtegerJson(carga, Sigiloso), ProtegerJson(resposta, Sigiloso));
    }

    private static bool NomeSensivel(string chave) =>
        ChaveSensivel.IsMatch(string.Concat(chave.Where(char.IsLetterOrDigit)));

    private static string ProtegerJson(ReadOnlyMemory<byte>? corpo, Func<string, bool> sigiloso)
    {
        if (corpo is null) return "null";
        try
        {
            using var json = JsonDocument.Parse(corpo.Value);
            return SerializarProtegido(json.RootElement, sigiloso, 0);
        }
        catch (Exception erro) when (erro is JsonException or InvalidOperationException or ArgumentException)
        {
            return "null";
        }
    }

    private static string SerializarProtegido(JsonElement elemento, Func<string, bool> sigiloso, int profundidade)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        EscreverProtegido(writer, elemento, sigiloso, profundidade);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void EscreverProtegido(Utf8JsonWriter writer, JsonElement elemento, Func<string, bool> sigiloso,
        int profundidade)
    {
        if (profundidade >= 64)
        {
            writer.WriteNullValue();
            return;
        }
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var propriedade in elemento.EnumerateObject())
                {
                    if (NomeSensivel(propriedade.Name) || sigiloso(propriedade.Name)) continue;
                    writer.WritePropertyName(propriedade.Name);
                    EscreverProtegido(writer, propriedade.Value, sigiloso, profundidade + 1);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in elemento.EnumerateArray())
                    EscreverProtegido(writer, item, sigiloso, profundidade + 1);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                var texto = elemento.GetString()!;
                var inicio = texto.AsSpan().TrimStart();
                if (!inicio.IsEmpty && inicio[0] is '{' or '[')
                {
                    try
                    {
                        using var interno = JsonDocument.Parse(texto);
                        writer.WriteStringValue(SerializarProtegido(interno.RootElement, sigiloso, profundidade + 1));
                        break;
                    }
                    catch (JsonException)
                    {
                        // O texto do ditado também pode começar com uma lacuna entre colchetes.
                    }
                }
                writer.WriteStringValue(sigiloso(texto) ? Oculto : texto);
                break;
            case JsonValueKind.Number when sigiloso(elemento.GetRawText()):
                writer.WriteStringValue(Oculto);
                break;
            default:
                elemento.WriteTo(writer);
                break;
        }
    }

    private string? SenhaDoBanco()
    {
        try
        {
            var conexao = new DbConnectionStringBuilder
            {
                ConnectionString = configuration.GetConnectionString("ConexaoPadrao")
            };
            return conexao.TryGetValue("Password", out var senha) || conexao.TryGetValue("Pwd", out senha)
                ? senha as string : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
