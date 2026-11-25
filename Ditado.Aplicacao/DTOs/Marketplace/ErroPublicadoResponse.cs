namespace Ditado.Aplicacao.DTOs.Marketplace;

public record ErroPublicadoResponse(
    int Id, string Codigo, string Nome, string Descricao,
    string? ExemploEsperado, string? ExemploResposta, string? ObservacaoPedagogica,
    string? SerieAno, int AutorId, bool Ativo, DateTime DataCriacao, DateTime DataAlteracao,
    string? AutorNome = null, string? NivelVisibilidade = null)
{
    public bool TemRegraExecutavel { get; init; }
    /// <summary>Categoria pedagógica do erro. Nula nos registros legados ainda sem mapeamento.</summary>
    public CategoriaPedagogicaDto? CategoriaPedagogica { get; init; }
    /// <summary>Pares em ordem de exibição. Os campos exemploEsperado e exemploResposta espelham o primeiro par.</summary>
    public IReadOnlyList<ExemploErroDto> Exemplos { get; init; } = [];
}
