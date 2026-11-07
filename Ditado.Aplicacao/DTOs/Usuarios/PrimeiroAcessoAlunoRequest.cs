using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class PrimeiroAcessoAlunoRequest
{
    [Required] public string MatriculaOuCodigo { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 4)] public string Pin { get; set; } = string.Empty;
}
