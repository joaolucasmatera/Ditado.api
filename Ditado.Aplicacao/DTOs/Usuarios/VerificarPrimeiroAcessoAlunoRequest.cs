using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class VerificarPrimeiroAcessoAlunoRequest
{
    [Required] public string MatriculaOuCodigo { get; set; } = string.Empty;
}
