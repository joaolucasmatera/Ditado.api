using System.Security.Claims;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/exportacoes")]
[Authorize(Roles = "Administrador,Professor")]
public sealed class ExportacoesController(ExportacaoService exportacao) : ControllerBase
{
    /// <summary>Exporta o CSV operacional de tentativas das escolas autorizadas.</summary>
    /// <remarks>
    /// Exige perfil de Administrador ou Professor e o escopo escolar é validado e auditado.
    /// A última coluna, classificacoes, é um array JSON com cada achado identificado por lacunaId,
    /// origem, tipoErro e erroCatalogoId. A categoria gravada aparece nos campos
    /// categoriaPedagogicaCodigo, categoriaPedagogicaNome e categoriaPedagogicaFamilia.
    /// Os campos de categoria ausentes são strings vazias e tentativas sem achados têm um array vazio.
    /// Use o cabeçalho X-Proximo-Cursor para avançar a partir do ID da última tentativa.
    /// </remarks>
    /// <param name="escolas">IDs das escolas separados por vírgula, como 1,2.</param>
    /// <param name="cursor">ID da última tentativa exportada na página anterior.</param>
    /// <param name="limiteLinhas">Máximo de tentativas por página, entre 1 e 10000.</param>
    /// <param name="limiteBytes">Limite de bytes da página, entre 1024 e 20000000.</param>
    /// <param name="tempoMaximoMs">Limite de tempo em milissegundos, entre 100 e 30000.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">CSV de tentativas com achados de Código e LLM.</response>
    /// <response code="400">Filtros inválidos, escopo escolar negado ou limites de exportação excedidos.</response>
    /// <response code="401">Autenticação necessária.</response>
    /// <response code="403">Perfil sem acesso à exportação operacional.</response>
    /// <response code="499">O cliente cancelou a transferência.</response>
    [HttpGet("respostas")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    public async Task<IActionResult> ExportarRespostas([FromQuery] string escolas, [FromQuery] int? cursor, [FromQuery] int limiteLinhas = 1000, [FromQuery] int limiteBytes = 5_000_000, [FromQuery] int tempoMaximoMs = 3000, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atorId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(escolas) || !escolas.Split(',').All(v => int.TryParse(v, out _))) return BadRequest(new { mensagem = "Informe escolas como lista de IDs inteiros." });
        try
        {
            var ids = escolas.Split(',').Select(int.Parse).Distinct().ToArray();
            var caminhoTemporario = Path.Combine(Path.GetTempPath(), $"ditado-export-{Guid.NewGuid():N}.csv");
            try
            {
                ExportacaoResumo resultado;
                await using (var arquivo = new FileStream(caminhoTemporario, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.SequentialScan))
                    resultado = await exportacao.EscreverRespostasCsvAsync(atorId, ids, new ExportacaoOpcoes { CursorId = cursor, LimiteLinhas = limiteLinhas, LimiteBytes = limiteBytes, TempoMaximoMs = tempoMaximoMs }, arquivo, cancellationToken);
                Response.ContentType = "text/csv; charset=utf-8";
                Response.Headers.Append("Content-Disposition", "attachment; filename=respostas.csv");
                Response.Headers.Append("X-Proximo-Cursor", resultado.ProximoCursor?.ToString() ?? string.Empty);
                await using var leitura = new FileStream(caminhoTemporario, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
                await leitura.CopyToAsync(Response.Body, cancellationToken);
            }
            finally
            {
                if (System.IO.File.Exists(caminhoTemporario)) System.IO.File.Delete(caminhoTemporario);
            }
            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            var ids = escolas.Split(',').Where(v => int.TryParse(v, out _)).Select(int.Parse).Distinct().ToArray();
            await exportacao.RegistrarExportacaoCanceladaAsync(atorId, ids);
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }
}
