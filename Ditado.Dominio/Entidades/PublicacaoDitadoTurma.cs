namespace Ditado.Dominio.Entidades;

public class PublicacaoDitadoTurma
{
    public int PublicacaoDitadoId { get; set; }
    public int TurmaId { get; set; }
    public PublicacaoDitado PublicacaoDitado { get; set; } = null!;
    public Turma Turma { get; set; } = null!;
}
