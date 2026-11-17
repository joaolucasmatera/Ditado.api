using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs;

public class CriarDitadoRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string TextoComMarcacoes { get; set; } = string.Empty;
    public string AudioBase64 { get; set; } = string.Empty;
    public List<int> CategoriaIds { get; set; } = new();
    [MinLength(1, ErrorMessage = "Selecione pelo menos um erro do catálogo.")]
    public List<int> ErroCatalogoIds { get; set; } = new();
}
