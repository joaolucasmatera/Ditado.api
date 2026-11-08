using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Services;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ditado.Testes.Services;

public class DitadoServiceTests_TiposErro : IDisposable
{
    private readonly DitadoDbContext _context;

    public DitadoServiceTests_TiposErro()
    {
        var options = new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DitadoDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void IdentificarTiposErro_TrocaSurdaSonora_AcumulaTipoEspecificoEOperacional()
    {
        Assert.Equal(
            new[] { TipoErro.TrocaGrafemasSurdaSonora, TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("vaca", "faca"));
    }

    [Theory]
    [InlineData("gto", "gato", TipoErro.OmissaoLetra)]
    [InlineData("gatto", "gato", TipoErro.AcrescimoLetra)]
    [InlineData("cato", "gato", TipoErro.SubstituicaoLetra)]
    public void IdentificarTiposErro_OperacaoObservavel_ClassificaSeparadamente(string resposta, string esperado, TipoErro tipo)
    {
        Assert.Contains(tipo, ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado));
    }

    [Theory]
    [InlineData("bulacha", "bolacha")]
    [InlineData("buneca", "boneca")]
    public void IdentificarTiposErro_TrocaVocalica_NaoInfereTranscricaoDaFala(string resposta, string esperado)
    {
        Assert.Equal(
            new[] { TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado));
    }

    [Fact]
    public void IdentificarTiposErro_TrocaVocalicaTonica_NaoClassificaComoTranscricaoFonetica()
    {
        Assert.Equal(
            new[] { TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("canita", "caneta"));
    }

    [Fact]
    public void IdentificarTiposErro_RrEntreVogais_AcumulaRegraContextualEOmissao()
    {
        Assert.Equal(
            new[] { TipoErro.RegraContextual, TipoErro.OmissaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("caro", "carro"));
    }

    [Fact]
    public void IdentificarTiposErro_RrEmPalavraLonga_AcumulaRegraContextualEOmissao()
    {
        Assert.Equal(
            new[] { TipoErro.RegraContextual, TipoErro.OmissaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("cachoro", "cachorro"));
    }

    [Fact]
    public void IdentificarTiposErro_GrafiaArbitrariaSZ_AcumulaTipoEspecificoESubstituicao()
    {
        Assert.Equal(
            new[] { TipoErro.GrafiaArbitrariaIrregular, TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("caza", "casa"));
    }

    [Fact]
    public void IdentificarTiposErro_GrafiaArbitrariaHInicial_AcumulaTipoEspecificoEOmissao()
    {
        Assert.Equal(
            new[] { TipoErro.GrafiaArbitrariaIrregular, TipoErro.OmissaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("omem", "homem"));
    }

    [Theory]
    [InlineData("nuve", "nuvem")]
    [InlineData("bano", "banho")]
    public void IdentificarTiposErro_MarcaNasalRemovida_AcumulaNasalizacaoEOmissao(string resposta, string esperado)
    {
        Assert.Equal(
            new[] { TipoErro.Nasalizacao, TipoErro.OmissaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado));
    }

    [Fact]
    public void IdentificarTiposErro_EncontroConsonantalSimplificado_AcumulaComplexidadeEOmissao()
    {
        Assert.Equal(
            new[] { TipoErro.OmissaoLetra, TipoErro.ComplexidadeSilabica },
            ClassificadorTiposErro.IdentificarTiposErro("pato", "prato"));
    }

    [Fact]
    public void IdentificarTiposErro_EncontroConsonantalInterno_AcumulaComplexidadeEOmissao()
    {
        Assert.Equal(
            new[] { TipoErro.OmissaoLetra, TipoErro.ComplexidadeSilabica },
            ClassificadorTiposErro.IdentificarTiposErro("compar", "comprar"));
    }

    [Theory]
    [InlineData("fr\u00E1gio", "fr\u00E1gil")]
    [InlineData("f\u00E1cio", "f\u00E1cil")]
    public void IdentificarTiposErro_TrocaDeFinalIlPorIo_NaoInfereHipercorrecao(string resposta, string esperado)
    {
        Assert.Equal(
            new[] { TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado));
    }

    [Fact]
    public void IdentificarTiposErro_OutraTrocaFinalDeL_MantemSomenteErroObservavel()
    {
        Assert.Equal(
            new[] { TipoErro.SubstituicaoLetra },
            ClassificadorTiposErro.IdentificarTiposErro("fr\u00E1gol", "fr\u00E1gil"));
    }

    [Fact]
    public void IdentificarTiposErro_AcentoIsolado_RetornaSomenteAcentuacao()
    {
        Assert.Equal(
            new[] { TipoErro.Acentuacao },
            ClassificadorTiposErro.IdentificarTiposErro("cafe", "caf\u00E9"));
    }

    [Fact]
    public void IdentificarTiposErro_Juntura_RetornaSomenteJuntura()
    {
        Assert.Equal(
            new[] { TipoErro.JunturaSegmentacao },
            ClassificadorTiposErro.IdentificarTiposErro("em bora", "embora"));
    }

    [Fact]
    public void IdentificarTiposErro_FormasVerbaisComRadicalESufixosConhecidos_RetornaConcordanciaFlexao()
    {
        Assert.Equal(
            new[] { TipoErro.ConcordanciaFlexao },
            ClassificadorTiposErro.IdentificarTiposErro("chegou", "chegaram"));
    }

    [Theory]
    [InlineData("maria", "Maria")]
    [InlineData("gato.", "gato")]
    public void IdentificarTiposErro_DiferencaDeCaixaOuPontuacao_RetornaTipoDeApresentacao(string resposta, string esperado)
    {
        Assert.Equal(
            new[] { TipoErro.MaiusculasPontuacao },
            ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado));
    }

    [Fact]
    public void IdentificarTiposErro_CaixaComAcentuacao_AcumulaOsDoisTipos()
    {
        Assert.Equal(
            new[] { TipoErro.Acentuacao, TipoErro.MaiusculasPontuacao },
            ClassificadorTiposErro.IdentificarTiposErro("Cafe", "caf\u00E9"));
    }

    [Fact]
    public void IdentificarTiposErro_CaixaComSubstituicao_AcumulaOsTresTiposAplicaveis()
    {
        Assert.Equal(
            new[] { TipoErro.GrafiaArbitrariaIrregular, TipoErro.SubstituicaoLetra, TipoErro.MaiusculasPontuacao },
            ClassificadorTiposErro.IdentificarTiposErro("Caza", "casa"));
    }

    [Fact]
    public void IdentificarTiposErro_CaixaComOmissaoEComplexidade_AcumulaOsTresTiposAplicaveis()
    {
        Assert.Equal(
            new[] { TipoErro.OmissaoLetra, TipoErro.ComplexidadeSilabica, TipoErro.MaiusculasPontuacao },
            ClassificadorTiposErro.IdentificarTiposErro("Pato", "prato"));
    }

    [Fact]
    public void IdentificarTiposErro_PalavraSemRegraDelimitada_RetornaVazio()
    {
        Assert.Empty(ClassificadorTiposErro.IdentificarTiposErro("janela", "tomate"));
    }

    [Fact]
    public void IdentificarTiposErro_RespostaExatamenteIgual_RetornaVazio()
    {
        Assert.Empty(ClassificadorTiposErro.IdentificarTiposErro("gato", "gato"));
    }

    [Fact]
    public void RespostaSegmento_PersisteMultiplosTiposErro()
    {
        var ditado = new Ditado.Dominio.Entidades.Ditado { Titulo = "Ditado teste" };
        var aluno = new Usuario
        {
            Nome = "Aluno teste",
            Login = "aluno-teste",
            SenhaHash = "hash",
            Tipo = TipoUsuario.Aluno,
            Ativo = true
        };
        _context.Ditados.Add(ditado);
        _context.Usuarios.Add(aluno);
        _context.SaveChanges();

        var segmento = new DitadoSegmento
        {
            DitadoId = ditado.Id,
            Ordem = 1,
            Tipo = TipoSegmento.Lacuna,
            Conteudo = "prato"
        };
        var respostaDitado = new RespostaDitado
        {
            DitadoId = ditado.Id,
            AlunoId = aluno.Id,
            Nota = 0
        };
        _context.DitadoSegmentos.Add(segmento);
        _context.RespostaDitados.Add(respostaDitado);
        _context.SaveChanges();

        var tipos = ClassificadorTiposErro.IdentificarTiposErro("pato", segmento.Conteudo);
        var resposta = new RespostaSegmento
        {
            RespostaDitadoId = respostaDitado.Id,
            SegmentoId = segmento.Id,
            RespostaFornecida = "pato",
            RespostaEsperada = segmento.Conteudo,
            Correto = false,
            EstadoClassificacao = EstadoClassificacao.Classificado,
            MetodoClassificacao = MetodoClassificacao.Codigo,
            TiposErro = tipos.Select(tipo => new RespostaSegmentoTipoErro { TipoErro = tipo }).ToList()
        };
        _context.RespostaSegmentos.Add(resposta);
        _context.SaveChanges();

        var persistida = _context.RespostaSegmentos
            .Include(item => item.TiposErro)
            .Single(item => item.Id == resposta.Id);

        Assert.Equal(2, persistida.TiposErro.Count);
        Assert.Contains(persistida.TiposErro, item => item.TipoErro == TipoErro.OmissaoLetra);
        Assert.Contains(persistida.TiposErro, item => item.TipoErro == TipoErro.ComplexidadeSilabica);
        Assert.Equal(EstadoClassificacao.Classificado, persistida.EstadoClassificacao);
        Assert.Equal("prato", persistida.RespostaEsperada);
        Assert.Equal(MetodoClassificacao.Codigo, persistida.MetodoClassificacao);
    }
}
