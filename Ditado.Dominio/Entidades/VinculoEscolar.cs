using Ditado.Dominio.Enums;

namespace Ditado.Dominio.Entidades;

public class VinculoEscolar
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int UsuarioId { get; set; }
    public PapelEscolar Papel { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? DataSaida { get; set; }

    public Escola Escola { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
