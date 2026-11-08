using System.Security.Claims;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/audio-retencao")]
[Authorize(Roles = "Administrador")]
public sealed class AudioRetencaoController(AudioRetencaoService audio) : ControllerBase
{
    [HttpGet("candidatos")]
    public async Task<ActionResult<IReadOnlyList<AudioLimpezaCandidato>>> Candidatos(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atorId)) return Unauthorized();
        try { return Ok(await audio.ListarCandidatosAsync(atorId, DateTime.UtcNow, cancellationToken)); }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message }); }
    }

    [HttpPost("confirmar")]
    public async Task<ActionResult<int>> Confirmar([FromBody] IReadOnlyCollection<int> ditadoIds, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atorId)) return Unauthorized();
        try { return Ok(await audio.ConfirmarLimpezaAsync(ditadoIds, atorId, DateTime.UtcNow, cancellationToken)); }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message }); }
    }
}
