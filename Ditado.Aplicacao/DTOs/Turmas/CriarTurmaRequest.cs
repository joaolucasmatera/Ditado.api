using System.ComponentModel.DataAnnotations;

namespace Ditado.Aplicacao.DTOs.Turmas;

public class CriarTurmaRequest
{
    [Required(ErrorMessage = "Nome da turma � obrigat�rio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "S�rie � obrigat�ria.")]
    [Range(1, 9, ErrorMessage = "S�rie deve ser de 1 a 9 (ensino fundamental).")]
    public int Serie { get; set; }

    [Required(ErrorMessage = "Ano letivo � obrigat�rio.")]
    [Range(2000, 2100, ErrorMessage = "Ano letivo inv�lido.")]
    public int AnoLetivo { get; set; }

    [StringLength(16, ErrorMessage = "Semestre n�o pode exceder 16 caracteres.")]
    public string? Semestre { get; set; }

    [StringLength(500, ErrorMessage = "Descri��o n�o pode exceder 500 caracteres.")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Professor respons�vel � obrigat�rio.")]
    public int ProfessorResponsavelId { get; set; }

    [Required(ErrorMessage = "Escola é obrigatória para novas turmas.")]
    public int? EscolaId { get; set; }

    public List<int> AlunosIds { get; set; } = new List<int>();
}
