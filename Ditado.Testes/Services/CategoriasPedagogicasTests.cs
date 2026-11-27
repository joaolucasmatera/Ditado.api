using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Xunit;

namespace Ditado.Testes.Services;

public class CategoriasPedagogicasTests
{
    [Theory]
    [InlineData(TipoErro.Acentuacao, "HIBRIDO_ACENTUACAO")]
    [InlineData(TipoErro.TrocaGrafemasSurdaSonora, "FONOLOGIA_SEGMENTO")]
    [InlineData(TipoErro.ComplexidadeSilabica, "FONOLOGIA_SILABA")]
    [InlineData(TipoErro.RegraContextual, "ORTOGRAFIA_CONTEXTUAL")]
    [InlineData(TipoErro.GrafiaArbitrariaIrregular, "ORTOGRAFIA_ARBITRARIA")]
    [InlineData(TipoErro.Nasalizacao, null)]
    [InlineData(TipoErro.OmissaoLetra, null)]
    [InlineData(TipoErro.AcrescimoLetra, null)]
    [InlineData(TipoErro.SubstituicaoLetra, null)]
    [InlineData(null, null)]
    public void RegraProgramadaRecebeSomenteCategoriaComCorrespondenciaConfiavel(TipoErro? regra, string? codigo)
    {
        var definicao = new ErroCatalogo { Nome = "Acentuação", RegraClassificacao = regra };
        Assert.Equal(codigo, CategoriasPedagogicas.DaDefinicao(definicao)?.Codigo);
    }

    [Fact]
    public void CategoriaExplicitaPrevaleceSobreMapaDaRegra()
    {
        var definicao = new ErroCatalogo
        {
            RegraClassificacao = TipoErro.Acentuacao,
            CategoriaPedagogicaCodigo = "FONOLOGIA_SILABA"
        };
        Assert.Equal("FONOLOGIA_SILABA", CategoriasPedagogicas.DaDefinicao(definicao)?.Codigo);
    }
}
