namespace Ditado.Dominio.Entidades;

public class ParticipacaoTurma
{
    public int Id { get; set; }
    public int TurmaId { get; set; }
    public int AlunoId { get; set; }
    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? DataSaida { get; set; }
    public bool Ativa { get; set; } = true;

    public Turma Turma { get; set; } = null!;
    public Usuario Aluno { get; set; } = null!;
}
