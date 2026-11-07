using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class PublicacaoErroCatalogo
{
    public int Id { get; set; }
    public int ErroCatalogoId { get; set; }
    public NivelVisibilidade Nivel { get; set; }
    public int? EscolaId { get; set; }
    public int Versao { get; set; }
    public int? ChaveVigente { get; set; }
    public DateTime VigenteDesde { get; set; } = DateTime.UtcNow;
    public DateTime? EncerradaEm { get; set; }
    public int CriadaPorId { get; set; }
    public int? EncerradaPorId { get; set; }
    public ErroCatalogo ErroCatalogo { get; set; } = null!;
    public Escola? Escola { get; set; }
    public ICollection<PublicacaoErroCatalogoTurma> TurmasAlvo { get; set; } = new List<PublicacaoErroCatalogoTurma>();
}
