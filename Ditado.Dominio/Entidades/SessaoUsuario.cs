namespace Ditado.Dominio.Entidades;

public class SessaoUsuario
{
    public Guid Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; }
    public DateTime? RevogadaEm { get; set; }
    public string? MotivoRevogacao { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
