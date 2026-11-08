namespace Ditado.Aplicacao.DTOs.Marketplace;

public record ErroPublicadoResponse(
    int Id, string Codigo, string Nome, string Descricao,
    string? ExemploEsperado, string? ExemploResposta, string? ObservacaoPedagogica,
    string? SerieAno, int AutorId, bool Ativo, DateTime DataCriacao, DateTime DataAlteracao)
{
    public bool TemRegraExecutavel => false;
}
