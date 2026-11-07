using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class LoginAlunoRequest
{
    [Required] public string MatriculaOuCodigo { get; set; } = string.Empty;
    [Required] public string Pin { get; set; } = string.Empty;
}
