using System.ComponentModel.DataAnnotations;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;

namespace Ditado.Aplicacao.Services;

public static class CategoriasPedagogicas
{
    private const string NaoOrtografico = "NAO_ORTOGRAFICO";

    // Saggiomo (2018), seções 3.3.2.1–3.3.2.3 e Quadro 16.
    public static IReadOnlyList<CategoriaPedagogicaDto> Opcoes { get; } = Array.AsReadOnly<CategoriaPedagogicaDto>(
    [
        new("FONOLOGIA_SEGMENTO", "Segmento", "Fonologia da língua"),
        new("FONOLOGIA_SILABA", "Sílaba", "Fonologia da língua"),
        new("HIBRIDO_ACENTUACAO", "Acentuação", "Híbrida"),
        new("HIBRIDO_SUPERGENERALIZACAO", "Supergeneralização", "Híbrida"),
        new("ORTOGRAFIA_ARBITRARIA", "Arbitrariedade", "Sistema ortográfico"),
        new("ORTOGRAFIA_CONTEXTUAL", "Regras contextuais", "Sistema ortográfico"),
        new(NaoOrtografico, "Não ortográfico", null)
    ]);

    internal static CategoriaPedagogicaDto? DaDefinicao(ErroCatalogo erro)
    {
        var codigo = erro.CategoriaPedagogicaCodigo ?? (erro.RegraClassificacao switch
        {
            TipoErro.Acentuacao => "HIBRIDO_ACENTUACAO",
            TipoErro.TrocaGrafemasSurdaSonora => "FONOLOGIA_SEGMENTO",
            TipoErro.ComplexidadeSilabica => "FONOLOGIA_SILABA",
            TipoErro.RegraContextual => "ORTOGRAFIA_CONTEXTUAL",
            TipoErro.GrafiaArbitrariaIrregular => "ORTOGRAFIA_ARBITRARIA",
            _ => null
        });
        return Obter(codigo, erro.CategoriaPedagogicaNome);
    }

    internal static (string Codigo, string? Nome) Validar(CategoriaPedagogicaRequest? categoria)
    {
        if (categoria is null)
            throw new ValidationException("Informe a categoria pedagógica do erro.");

        var codigo = categoria.Codigo?.Trim();
        var opcao = Opcoes.FirstOrDefault(c => c.Codigo == codigo)
            ?? throw new ValidationException("Selecione uma categoria pedagógica válida.");
        var nome = categoria.Nome?.Trim();
        if (opcao.Codigo == NaoOrtografico)
        {
            if (string.IsNullOrEmpty(nome) || nome.Length > 200)
                throw new ValidationException("Informe o nome da categoria não ortográfica, com até 200 caracteres.");
            return (opcao.Codigo, nome);
        }

        if (!string.IsNullOrEmpty(nome))
            throw new ValidationException("Selecione a categoria ortográfica na lista.");
        return (opcao.Codigo, null);
    }

    internal static CategoriaPedagogicaDto? Obter(string? codigo, string? nome)
    {
        var opcao = Opcoes.FirstOrDefault(c => c.Codigo == codigo);
        return opcao is { Codigo: NaoOrtografico } && nome is not null ? opcao with { Nome = nome } : opcao;
    }
}
