namespace Ditado.Aplicacao.DTOs;

/// <summary>
/// Representação completa de um ditado para consulta por professores e administradores.
/// Inclui o conteúdo das lacunas, diferentemente do DTO usado na realização pelo aluno.
/// </summary>
public class DitadoCompletoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public int? AutorId { get; set; }
    public string? AutorNome { get; set; }
    public List<CategoriaSimplificadaDto> Categorias { get; set; } = new();
    public string AudioBase64 { get; set; } = string.Empty;
    public List<SegmentoCompletoDto> Segmentos { get; set; } = new();
}

/// <summary>Segmento do ditado; lacunas incluem o gabarito para visualização de staff.</summary>
public class SegmentoCompletoDto
{
    public int Id { get; set; }
    public int Ordem { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
}
