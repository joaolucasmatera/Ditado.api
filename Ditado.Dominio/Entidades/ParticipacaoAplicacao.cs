namespace Ditado.Dominio.Entidades;

public class ParticipacaoAplicacao
{
    public int Id { get; set; }
    public int AplicacaoDitadoId { get; set; }
    public int AlunoId { get; set; }
    public DateTime ConcedidaEm { get; set; } = DateTime.UtcNow;
    public DateTime? RevogadaEm { get; set; }

    public AplicacaoDitado AplicacaoDitado { get; set; } = null!;
    public Usuario Aluno { get; set; } = null!;
}
