using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ditado.Aplicacao.DTOs.Marketplace;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CategoriaPedagogicaRequest
{
    [Required, StringLength(40)]
    public string Codigo { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Nome { get; set; }
}

public sealed record CategoriaPedagogicaDto(string Codigo, string Nome, string? Familia);
