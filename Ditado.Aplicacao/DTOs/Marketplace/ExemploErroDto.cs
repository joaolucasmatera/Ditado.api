using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ditado.Aplicacao.DTOs.Marketplace;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ExemploErroDto
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Required, StringLength(2000)]
    public string Esperado { get; init; } = string.Empty;
    [Required, StringLength(2000)]
    public string Resposta { get; init; } = string.Empty;

    public ExemploErroDto() { }

    public ExemploErroDto(string esperado, string resposta) => (Esperado, Resposta) = (esperado, resposta);

    internal static ExemploErroDto[] LerAdicionais(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<ExemploErroDto[]>(json, Json)!;

    internal static ExemploErroDto[] Ler(string? esperado, string? resposta, string? adicionais)
    {
        var outros = LerAdicionais(adicionais);
        return string.IsNullOrWhiteSpace(esperado) || string.IsNullOrWhiteSpace(resposta)
            ? outros
            : [new(esperado, resposta), ..outros];
    }

    internal static string? GravarAdicionais(ExemploErroDto[] exemplos) =>
        exemplos.Length <= 1 ? null : JsonSerializer.Serialize(exemplos[1..], Json);
}
