using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class SolicitarAcessoAlunoRequest
{
    [StringLength(200)] public string? Nome { get; set; }
    [Required, StringLength(20)] public string Matricula { get; set; } = string.Empty;
}
