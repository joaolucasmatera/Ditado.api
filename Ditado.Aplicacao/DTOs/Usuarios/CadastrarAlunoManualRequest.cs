using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class CadastrarAlunoManualRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Nome { get; set; } = string.Empty;
    [Range(1, 999)] public int NumeroChamada { get; set; }
    [StringLength(20)] public string? Matricula { get; set; }
    [Range(1, int.MaxValue)] public int TurmaId { get; set; }
}
