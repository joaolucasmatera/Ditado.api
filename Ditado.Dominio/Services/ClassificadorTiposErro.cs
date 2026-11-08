using Ditado.Dominio.Enums;
using System.Globalization;
using System.Text;

namespace Ditado.Dominio.Services;

public static class ClassificadorTiposErro
{
    private static readonly TipoErro[] OrdemDaTabela =
    [
        TipoErro.TranscricaoFonetica,
        TipoErro.TrocaGrafemasSurdaSonora,
        TipoErro.RegraContextual,
        TipoErro.GrafiaArbitrariaIrregular,
        TipoErro.Nasalizacao,
        TipoErro.OmissaoLetra,
        TipoErro.AcrescimoLetra,
        TipoErro.SubstituicaoLetra,
        TipoErro.ComplexidadeSilabica,
        TipoErro.HipercorrecaoSupergeneralizacao,
        TipoErro.Acentuacao,
        TipoErro.JunturaSegmentacao,
        TipoErro.ConcordanciaFlexao,
        TipoErro.MaiusculasPontuacao
    ];

    private static readonly HashSet<string> EncontrosConsonantais =
    ["br", "cr", "dr", "fr", "gr", "pr", "tr", "bl", "cl", "fl", "gl", "pl"];

    public static IReadOnlyList<TipoErro> IdentificarTiposErro(string resposta, string esperado)
    {
        var respostaOriginal = resposta.Trim();
        var esperadoOriginal = esperado.Trim();
        if (string.Equals(respostaOriginal, esperadoOriginal, StringComparison.Ordinal))
            return Array.Empty<TipoErro>();

        var respostaNormalizada = respostaOriginal.ToLowerInvariant();
        var esperadoNormalizado = esperadoOriginal.ToLowerInvariant();
        var tipos = new HashSet<TipoErro>();

        var diferencaApenasDeCaixaOuPontuacao = MesmoTextoSemCaixaOuPontuacao(respostaOriginal, esperadoOriginal);
        var diferencaDeCaixaComOutraEdicao = TemDiferencaDeCaixaEmEdicaoUnica(
            RemoverPontuacao(esperadoOriginal),
            RemoverPontuacao(respostaOriginal));
        if (diferencaApenasDeCaixaOuPontuacao || diferencaDeCaixaComOutraEdicao)
            tipos.Add(TipoErro.MaiusculasPontuacao);

        if (respostaNormalizada != esperadoNormalizado && MesmoTextoSemEspacos(respostaNormalizada, esperadoNormalizado))
            tipos.Add(TipoErro.JunturaSegmentacao);

        var mesmaPalavraSemAcentos = RemoverDiacriticos(respostaNormalizada) == RemoverDiacriticos(esperadoNormalizado);
        if (mesmaPalavraSemAcentos && respostaNormalizada != esperadoNormalizado)
        {
            tipos.Add(TipoErro.Acentuacao);
            if (DiferencaEnvolveVogalNasal(respostaNormalizada, esperadoNormalizado))
                tipos.Add(TipoErro.Nasalizacao);
        }

        if (string.IsNullOrWhiteSpace(respostaNormalizada) && !string.IsNullOrWhiteSpace(esperadoNormalizado))
        {
            tipos.Add(TipoErro.OmissaoLetra);
        }
        else if (!diferencaApenasDeCaixaOuPontuacao &&
                 !tipos.Contains(TipoErro.JunturaSegmentacao) &&
                 !mesmaPalavraSemAcentos &&
                 TryIdentificarEdicaoUnica(esperadoNormalizado, respostaNormalizada, out var edicao))
        {
            tipos.Add(DeterminarTipoOperacao(edicao.Tipo));
            AdicionarTipoPorEdicao(esperadoNormalizado, respostaNormalizada, edicao, tipos);
        }

        if (EhConcordanciaFlexao(esperadoNormalizado, respostaNormalizada))
            tipos.Add(TipoErro.ConcordanciaFlexao);

        return OrdemDaTabela.Where(tipos.Contains).ToArray();
    }

    private static void AdicionarTipoPorEdicao(string esperado, string resposta, Edicao edicao, HashSet<TipoErro> tipos)
    {
        if (edicao.Tipo == TipoEdicao.Substituicao)
        {
            if (EhTrocaSurdaSonora(esperado, edicao.Indice, edicao.CaractereEsperado, edicao.CaractereResposta))
                tipos.Add(TipoErro.TrocaGrafemasSurdaSonora);
        }

        if (EhGrafiaArbitraria(esperado, resposta, edicao))
            tipos.Add(TipoErro.GrafiaArbitrariaIrregular);

        if (EhRegraContextualR(esperado, resposta))
            tipos.Add(TipoErro.RegraContextual);

        if (EhNasalizacao(esperado, resposta, edicao))
            tipos.Add(TipoErro.Nasalizacao);

        if (EhComplexidadeSilabica(esperado, resposta, edicao))
            tipos.Add(TipoErro.ComplexidadeSilabica);

    }

    private static TipoErro DeterminarTipoOperacao(TipoEdicao tipo) => tipo switch
    {
        TipoEdicao.Remocao => TipoErro.OmissaoLetra,
        TipoEdicao.Adicao => TipoErro.AcrescimoLetra,
        TipoEdicao.Substituicao => TipoErro.SubstituicaoLetra,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Operacao de edicao nao reconhecida.")
    };

    private static bool TryIdentificarEdicaoUnica(string esperado, string resposta, out Edicao edicao)
    {
        edicao = default;
        if (esperado.Length == resposta.Length)
        {
            var indiceDiferente = -1;
            for (var i = 0; i < esperado.Length; i++)
            {
                if (esperado[i] == resposta[i])
                    continue;

                if (indiceDiferente >= 0)
                    return false;

                indiceDiferente = i;
            }

            if (indiceDiferente < 0)
                return false;

            edicao = new Edicao(TipoEdicao.Substituicao, indiceDiferente, esperado[indiceDiferente], resposta[indiceDiferente]);
            return true;
        }

        var textoMaior = esperado.Length > resposta.Length ? esperado : resposta;
        var textoMenor = esperado.Length > resposta.Length ? resposta : esperado;
        if (textoMaior.Length != textoMenor.Length + 1)
            return false;

        var indiceMaior = 0;
        var indiceMenor = 0;
        var indiceEdicao = -1;
        while (indiceMaior < textoMaior.Length && indiceMenor < textoMenor.Length)
        {
            if (textoMaior[indiceMaior] == textoMenor[indiceMenor])
            {
                indiceMaior++;
                indiceMenor++;
                continue;
            }

            if (indiceEdicao >= 0)
                return false;

            indiceEdicao = indiceMaior++;
        }

        if (indiceEdicao < 0)
            indiceEdicao = textoMaior.Length - 1;

        var ehRemocao = esperado.Length > resposta.Length;
        edicao = ehRemocao
            ? new Edicao(TipoEdicao.Remocao, indiceEdicao, esperado[indiceEdicao], '\0')
            : new Edicao(TipoEdicao.Adicao, indiceEdicao, '\0', resposta[indiceEdicao]);
        return true;
    }

    private static bool EhTrocaSurdaSonora(string texto, int indice, char esperado, char resposta)
    {
        var parConsonantal = (esperado, resposta) is ('p', 'b') or ('b', 'p') or ('t', 'd') or ('d', 't') or ('f', 'v') or ('v', 'f');
        if (parConsonantal)
            return true;

        if ((esperado, resposta) is ('s', 'z') or ('z', 's'))
            return indice == 0;

        if ((esperado, resposta) is ('c', 'g') or ('g', 'c'))
            return indice + 1 < texto.Length && texto[indice + 1] is 'a' or 'o' or 'u';

        return false;
    }

    private static bool EhGrafiaArbitraria(string esperadoTexto, string respostaTexto, Edicao edicao)
    {
        var caractereEsperado = edicao.CaractereEsperado;
        var caractereResposta = edicao.CaractereResposta;
        var texto = esperadoTexto;
        if (edicao.Tipo == TipoEdicao.Adicao)
        {
            caractereEsperado = '\0';
            caractereResposta = respostaTexto[edicao.Indice];
            texto = respostaTexto;
        }
        else if (edicao.Tipo == TipoEdicao.Remocao)
        {
            caractereEsperado = esperadoTexto[edicao.Indice];
            caractereResposta = '\0';
        }

        if ((caractereEsperado == 'h' || caractereResposta == 'h') && edicao.Indice == 0)
            return true;

        if ((caractereEsperado, caractereResposta) is ('s', 'z') or ('z', 's'))
        {
            var indice = edicao.Indice;
            var finalDaPalavra = indice == texto.Length - 1;
            var entreVogais = indice > 0 && indice + 1 < texto.Length &&
                              EhVogal(texto[indice - 1]) && EhVogal(texto[indice + 1]);
            return finalDaPalavra || entreVogais;
        }

        if ((caractereEsperado, caractereResposta) is ('c', 's') or ('s', 'c'))
            return edicao.Indice + 1 < texto.Length && texto[edicao.Indice + 1] is 'e' or 'i';

        return false;
    }

    private static bool EhRegraContextualR(string esperado, string resposta)
    {
        return RemoverUmRrEntreVogais(esperado).Contains(resposta, StringComparer.Ordinal) ||
               RemoverUmRrEntreVogais(resposta).Contains(esperado, StringComparer.Ordinal);
    }

    private static IEnumerable<string> RemoverUmRrEntreVogais(string texto)
    {
        for (var i = 1; i + 2 < texto.Length; i++)
        {
            if (texto[i] == 'r' && texto[i + 1] == 'r' && EhVogal(texto[i - 1]) && EhVogal(texto[i + 2]))
                yield return texto.Remove(i + 1, 1);
        }
    }

    private static bool EhNasalizacao(string esperado, string resposta, Edicao edicao)
    {
        if (edicao.Tipo == TipoEdicao.Substituicao &&
            (edicao.CaractereEsperado, edicao.CaractereResposta) is ('a', 'ã') or ('ã', 'a') or ('o', 'õ') or ('õ', 'o'))
            return true;

        var textoMarcador = edicao.Tipo == TipoEdicao.Adicao ? resposta : esperado;
        var indiceMarcador = edicao.Indice;
        var marcador = edicao.Tipo == TipoEdicao.Adicao ? edicao.CaractereResposta : edicao.CaractereEsperado;
        if (marcador is 'm' or 'n' && indiceMarcador < textoMarcador.Length &&
            EhVogal(textoMarcador[indiceMarcador - 1 < 0 ? 0 : indiceMarcador - 1]))
        {
            var depois = indiceMarcador + 1 < textoMarcador.Length ? textoMarcador[indiceMarcador + 1] : '\0';
            if (depois == '\0' || !EhVogal(depois))
                return true;
        }

        var textoDigrafo = edicao.Tipo == TipoEdicao.Adicao ? resposta : esperado;
        if (edicao.Tipo is TipoEdicao.Remocao or TipoEdicao.Adicao && marcador == 'h' && indiceMarcador > 0 &&
            textoDigrafo[indiceMarcador - 1] == 'n' && indiceMarcador > 1 &&
            EhVogal(textoDigrafo[indiceMarcador - 2]))
            return true;

        if (edicao.Tipo == TipoEdicao.Substituicao &&
            edicao.CaractereEsperado is 'm' or 'n' && edicao.CaractereResposta is 'm' or 'n')
        {
            var contextoEsperado = new Edicao(TipoEdicao.Remocao, edicao.Indice, edicao.CaractereEsperado, '\0');
            var contextoResposta = new Edicao(TipoEdicao.Remocao, edicao.Indice, edicao.CaractereResposta, '\0');
            return EhNasalizacao(esperado, resposta, contextoEsperado) && EhNasalizacao(resposta, esperado, contextoResposta);
        }

        return false;
    }

    private static bool EhComplexidadeSilabica(string esperado, string resposta, Edicao edicao)
    {
        return edicao.Tipo switch
        {
            TipoEdicao.Remocao => RemocaoQueSimplificaEncontro(esperado, resposta, edicao.Indice),
            TipoEdicao.Adicao => RemocaoQueSimplificaEncontro(resposta, esperado, edicao.Indice),
            _ => false
        };
    }

    private static bool RemocaoQueSimplificaEncontro(string palavraComEncontro, string palavraSimplificada, int indice)
    {
        if (indice <= 0 || indice + 1 >= palavraComEncontro.Length)
            return false;

        var inicioDoEncontro = indice - 1;
        var encontro = palavraComEncontro.Substring(inicioDoEncontro, 2);
        var seguidoPorVogal = EhVogal(palavraComEncontro[indice + 1]);
        return seguidoPorVogal && EncontrosConsonantais.Contains(encontro) &&
               palavraComEncontro.Remove(indice, 1) == palavraSimplificada;
    }

    private static bool EhConcordanciaFlexao(string esperado, string resposta)
    {
        return MesmoRadicalComSufixo(esperado, resposta, "aram", "ou") ||
               MesmoRadicalComSufixo(esperado, resposta, "eram", "eu") ||
               MesmoRadicalComSufixo(esperado, resposta, "iram", "iu") ||
               MesmoRadicalComSufixo(resposta, esperado, "aram", "ou") ||
               MesmoRadicalComSufixo(resposta, esperado, "eram", "eu") ||
               MesmoRadicalComSufixo(resposta, esperado, "iram", "iu");
    }

    private static bool MesmoRadicalComSufixo(string formaPlural, string formaSingular, string sufixoPlural, string sufixoSingular)
    {
        return formaPlural.Length > sufixoPlural.Length + 1 &&
               formaSingular.Length > sufixoSingular.Length + 1 &&
               formaPlural.EndsWith(sufixoPlural, StringComparison.Ordinal) &&
               formaSingular.EndsWith(sufixoSingular, StringComparison.Ordinal) &&
               formaPlural[..^sufixoPlural.Length] == formaSingular[..^sufixoSingular.Length];
    }

    private static bool MesmoTextoSemCaixaOuPontuacao(string resposta, string esperado)
    {
        var respostaSemPontuacao = RemoverPontuacao(resposta);
        var esperadoSemPontuacao = RemoverPontuacao(esperado);
        return string.Equals(respostaSemPontuacao, esperadoSemPontuacao, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TemDiferencaDeCaixaEmEdicaoUnica(string esperado, string resposta)
    {
        var esperadoSemAcentos = RemoverDiacriticos(esperado);
        var respostaSemAcentos = RemoverDiacriticos(resposta);

        if (esperadoSemAcentos.Length == respostaSemAcentos.Length)
        {
            return AlinharCaracteresParaCaixa(esperadoSemAcentos, respostaSemAcentos, -1);
        }

        var textoMaior = esperadoSemAcentos.Length > respostaSemAcentos.Length ? esperadoSemAcentos : respostaSemAcentos;
        var textoMenor = esperadoSemAcentos.Length > respostaSemAcentos.Length ? respostaSemAcentos : esperadoSemAcentos;
        if (textoMaior.Length != textoMenor.Length + 1)
            return false;

        for (var indiceIgnorado = 0; indiceIgnorado < textoMaior.Length; indiceIgnorado++)
        {
            var diferencasDeLetra = 0;
            var diferencaDeCaixa = false;
            var indiceMenor = 0;

            for (var indiceMaior = 0; indiceMaior < textoMaior.Length; indiceMaior++)
            {
                if (indiceMaior == indiceIgnorado)
                    continue;

                var caractereMaior = textoMaior[indiceMaior];
                var caractereMenor = textoMenor[indiceMenor++];
                if (char.ToLowerInvariant(caractereMaior) != char.ToLowerInvariant(caractereMenor))
                {
                    diferencasDeLetra++;
                    continue;
                }

                diferencaDeCaixa |= char.IsUpper(caractereMaior) != char.IsUpper(caractereMenor);
            }

            if (diferencasDeLetra <= 1 && diferencaDeCaixa)
                return true;
        }

        return false;
    }

    private static bool AlinharCaracteresParaCaixa(string esperado, string resposta, int indiceIgnorado)
    {
        var diferencasDeLetra = 0;
        var diferencaDeCaixa = false;

        for (var indice = 0; indice < esperado.Length; indice++)
        {
            if (indice == indiceIgnorado)
                continue;

            if (char.ToLowerInvariant(esperado[indice]) != char.ToLowerInvariant(resposta[indice]))
            {
                diferencasDeLetra++;
                continue;
            }

            diferencaDeCaixa |= char.IsUpper(esperado[indice]) != char.IsUpper(resposta[indice]);
        }

        return diferencasDeLetra <= 1 && diferencaDeCaixa;
    }

    private static bool MesmoTextoSemEspacos(string resposta, string esperado)
    {
        return string.Concat(resposta.Where(c => !char.IsWhiteSpace(c))) ==
               string.Concat(esperado.Where(c => !char.IsWhiteSpace(c)));
    }

    private static string RemoverPontuacao(string texto)
    {
        return string.Concat(texto.Where(c => !char.IsPunctuation(c) && !char.IsSymbol(c)));
    }

    private static string RemoverDiacriticos(string texto)
    {
        var decomposicao = texto.Normalize(NormalizationForm.FormD);
        return string.Concat(decomposicao.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC);
    }

    private static bool DiferencaEnvolveVogalNasal(string primeira, string segunda)
    {
        return primeira.Zip(segunda).Any(par => (par.First, par.Second) is ('a', 'ã') or ('ã', 'a') or ('o', 'õ') or ('õ', 'o'));
    }

    private static bool EhVogal(char caractere)
    {
        return "aeiouáéíóúâêôãõà".Contains(char.ToLowerInvariant(caractere));
    }

    private enum TipoEdicao
    {
        Remocao,
        Adicao,
        Substituicao
    }

    private readonly record struct Edicao(TipoEdicao Tipo, int Indice, char CaractereEsperado, char CaractereResposta);
}
