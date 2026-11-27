namespace Ditado.Aplicacao.DTOs.Professores;

public class ErrosPorTipoDto
{
    public int? ErroCatalogoId { get; set; }
    public string Origem { get; set; } = "Codigo";
    public int TipoErroId { get; set; } // ID do enum (0, 1, 2, etc.)
    public string Descricao { get; set; } = string.Empty; // "Erro de acentuação"
    public string DescricaoCurta { get; set; } = string.Empty; // "Acentuação"
    public int Quantidade { get; set; }
}
