namespace Ditado.Dominio.Entidades;

public class PublicacaoErroCatalogoTurma
{
    public int PublicacaoErroCatalogoId { get; set; }
    public int TurmaId { get; set; }
    public PublicacaoErroCatalogo PublicacaoErroCatalogo { get; set; } = null!;
    public Turma Turma { get; set; } = null!;
}
