namespace Ditado.Dominio.Entidades;

public class ErroCatalogo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? ExemploEsperado { get; set; }
    public string? ExemploResposta { get; set; }
    public string? ObservacaoPedagogica { get; set; }
    public string? SerieAno { get; set; }
    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;
    public bool Ativo { get; set; } = true;
    public DateTime? ArquivadoEm { get; set; }
    public int? ArquivadoPorId { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAlteracao { get; set; }
    public ICollection<PublicacaoErroCatalogo> Publicacoes { get; set; } = new List<PublicacaoErroCatalogo>();
}
