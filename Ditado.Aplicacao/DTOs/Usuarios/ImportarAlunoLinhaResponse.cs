namespace Ditado.Aplicacao.DTOs.Usuarios;

public sealed class ImportarAlunoLinhaResponse
{
    public int Linha { get; init; }
    public string? Nome { get; init; }
    public int? NumeroChamada { get; init; }
    public string? Matricula { get; init; }
    public string? Turma { get; init; }
    public bool Valida { get; init; }
    public string[] Pendencias { get; init; } = [];
}
