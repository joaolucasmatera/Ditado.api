using System.Security.Claims;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/ErrosCatalogo")]
[Authorize(Roles = "Administrador,Professor")]
public class ErrosCatalogoController(MarketplaceService marketplace) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ErroPublicadoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ErroPublicadoResponse>> Publicar(PublicarErroRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var autorId) || autorId <= 0)
            return Unauthorized();
        var publicado = await marketplace.PublicarAsync(request, autorId, cancellationToken);
        return CreatedAtAction(nameof(ObterRecibo), new { id = publicado.Id }, publicado);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ErroPublicadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ErroPublicadoResponse>> ObterRecibo(int id, CancellationToken cancellationToken)
    {
        var recibo = await marketplace.ObterReciboAsync(id, cancellationToken);
        return recibo is null ? NotFound() : Ok(recibo);
    }
}
