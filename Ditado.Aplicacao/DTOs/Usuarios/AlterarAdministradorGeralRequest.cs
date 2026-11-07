namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class AlterarAdministradorGeralRequest
{
    public bool Habilitar { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? CorrelacaoId { get; set; }
}
