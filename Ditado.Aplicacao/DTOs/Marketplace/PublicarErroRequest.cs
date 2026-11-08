using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ditado.Aplicacao.DTOs.Marketplace;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class PublicarErroRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe o nome do erro."), StringLength(200)]
    public string Nome { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a descrição do erro."), StringLength(10000)]
    public string Descricao { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? ExemploEsperado { get; set; }
    [StringLength(2000)]
    public string? ExemploResposta { get; set; }
    [StringLength(10000)]
    public string? ObservacaoPedagogica { get; set; }
    [StringLength(200)]
    public string? SerieAno { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ExemploEsperado) != string.IsNullOrWhiteSpace(ExemploResposta))
            yield return new ValidationResult("Preencha a escrita esperada e a resposta do aluno para informar um exemplo.",
                [nameof(ExemploEsperado), nameof(ExemploResposta)]);
    }
}
