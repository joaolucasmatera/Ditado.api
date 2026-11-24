using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Ditado.Dominio.Enums;

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
    /// <summary>Até cinco pares opcionais em ordem de exibição. A lista substitui os campos legados.</summary>
    [MaxLength(5, ErrorMessage = "Informe até 5 exemplos.")]
    public List<ExemploErroDto>? Exemplos { get; set; }
    [StringLength(10000)]
    public string? ObservacaoPedagogica { get; set; }
    [StringLength(200)]
    public string? SerieAno { get; set; }
    public NivelVisibilidade Nivel { get; set; } = NivelVisibilidade.Instituicao;
    public int? EscolaId { get; set; }
    public List<int> TurmaIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Exemplos is null && string.IsNullOrWhiteSpace(ExemploEsperado) != string.IsNullOrWhiteSpace(ExemploResposta))
            yield return new ValidationResult("Preencha a escrita esperada e a resposta do aluno para informar um exemplo.",
                [nameof(ExemploEsperado), nameof(ExemploResposta)]);

        if (Exemplos is null) yield break;
        for (var i = 0; i < Exemplos.Count; i++)
        {
            var exemplo = Exemplos[i];
            if (exemplo is null || string.IsNullOrWhiteSpace(exemplo.Esperado) || string.IsNullOrWhiteSpace(exemplo.Resposta))
                yield return new ValidationResult("Preencha a escrita esperada e a resposta do aluno.",
                    [$"{nameof(Exemplos)}[{i}]"]);
            else if (exemplo.Esperado.Length > 2000 || exemplo.Resposta.Length > 2000)
                yield return new ValidationResult("Cada campo do exemplo aceita até 2000 caracteres.",
                    [$"{nameof(Exemplos)}[{i}]"]);
        }
    }
}
