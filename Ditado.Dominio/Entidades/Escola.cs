namespace Ditado.Dominio.Entidades;

public class Escola
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<VinculoEscolar> Vinculos { get; set; } = new List<VinculoEscolar>();
}
