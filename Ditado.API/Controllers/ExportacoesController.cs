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
    [HttpGet("respostas")]
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
