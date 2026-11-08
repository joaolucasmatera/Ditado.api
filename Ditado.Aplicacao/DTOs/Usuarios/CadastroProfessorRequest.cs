using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class CadastroProfessorRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Nome { get; set; } = string.Empty;
    [Required, EmailAddress] public string Login { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)] public string Senha { get; set; } = string.Empty;
    [Required] public string OpcaoEscola { get; set; } = string.Empty;
    public int? EscolaId { get; set; }
    [StringLength(200)] public string? NomeNovaEscola { get; set; }
}
