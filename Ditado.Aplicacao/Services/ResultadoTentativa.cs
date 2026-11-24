using Ditado.Aplicacao.DTOs;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

internal static class ResultadoTentativa
{
    public static IQueryable<RespostaDitado> ComDetalhes(IQueryable<RespostaDitado> tentativas) =>
        tentativas
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.Segmento)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.TiposErro).ThenInclude(t => t.ErroCatalogo)
            .Include(r => r.RespostasSegmentos).ThenInclude(s => s.AnaliseLlm).ThenInclude(a => a!.Candidatos)
            .AsSplitQuery();

    public static IEnumerable<ClassificacaoCatalogoDto> Classificacoes(RespostaSegmento resposta) =>
        resposta.TiposErro.Where(t => t.ErroCatalogo != null)
            .Select(t => new ClassificacaoCatalogoDto(t.ErroCatalogoId!.Value, t.ErroCatalogo!.Codigo, t.ErroCatalogo.Nome, (int)t.TipoErro))
            .Concat(resposta.AnaliseLlm?.Candidatos.Where(c => c.Estado == LlmCandidatoEstado.Correspondente)
                .Select(c => new ClassificacaoCatalogoDto(c.ErroCatalogoId, c.Codigo, c.Nome, null, "LLM")) ?? []);

    public static ResultadoDitadoResponse Mapear(RespostaDitado tentativa)
    {
        var detalhes = tentativa.RespostasSegmentos.OrderBy(s => s.Segmento.Ordem).Select(s => new DetalheRespostaDto
        {
            SegmentoId = s.SegmentoId,
            RespostaFornecida = s.RespostaFornecida,
            RespostaEsperada = s.RespostaEsperada ?? s.Segmento.Conteudo,
            RespostaAusente = s.RespostaAusente,
            Correto = s.Correto,
            EstadoClassificacao = s.EstadoClassificacao.ToString(),
            MetodoClassificacao = s.MetodoClassificacao?.ToString(),
            EstadoAnaliseLlm = s.AnaliseLlm?.Estado.ToString(),
            TiposErro = s.TiposErro.Select(t => t.TipoErro.ObterDescricao())
                .Concat(s.TipoErro is { } legado ? [legado.ObterDescricao()] : Array.Empty<string>()).Distinct().ToList(),
            Classificacoes = Classificacoes(s).ToList()
        }).ToList();
        return new ResultadoDitadoResponse
        {
            RespostaDitadoId = tentativa.Id, Nota = tentativa.Nota, TotalLacunas = detalhes.Count,
            Acertos = detalhes.Count(d => d.Correto), Erros = detalhes.Count(d => !d.Correto), Detalhes = detalhes
        };
    }
}
