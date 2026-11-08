using Ditado.Dominio.Enums;

namespace Ditado.Aplicacao.DTOs;

public sealed class AlterarVisibilidadeRequest
{
    public NivelVisibilidade Nivel { get; set; }
    public int? EscolaId { get; set; }
    public List<int> TurmaIds { get; set; } = new();
}
