namespace Ditado.Dominio.Entidades;

public class AplicacaoDitado
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int TurmaId { get; set; }
    public int DitadoId { get; set; }
    public DateTime DataAtribuicao { get; set; } = DateTime.UtcNow;
    public DateTime? DataLimite { get; set; }
    public bool Ativa { get; set; } = true;

    public Escola Escola { get; set; } = null!;
    public Turma Turma { get; set; } = null!;
    public Ditado Ditado { get; set; } = null!;
    public ICollection<ParticipacaoAplicacao> Participacoes { get; set; } = new List<ParticipacaoAplicacao>();
    public ICollection<AplicacaoErroCatalogo> ErrosSelecionados { get; set; } = new List<AplicacaoErroCatalogo>();
    public ICollection<RespostaDitado> Tentativas { get; set; } = new List<RespostaDitado>();
}
