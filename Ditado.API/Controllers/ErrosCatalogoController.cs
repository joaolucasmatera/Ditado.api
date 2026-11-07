using System.Security.Claims;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/ErrosCatalogo")]
[Authorize(Roles = "Administrador,Professor")]
public class ErrosCatalogoController(MarketplaceService marketplace, VisibilidadeService visibilidade) : ControllerBase
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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
        var tipo = Enum.Parse<Dominio.Enums.TipoUsuario>(User.FindFirstValue(ClaimTypes.Role) ?? "Aluno");
        var recibo = await marketplace.ObterReciboAsync(id, usuarioId, tipo, cancellationToken);
        return recibo is null ? NotFound() : Ok(recibo);
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Professor,Aluno")]
    public async Task<ActionResult<List<ErroPublicadoResponse>>> Listar(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
        var tipo = Enum.Parse<Dominio.Enums.TipoUsuario>(User.FindFirstValue(ClaimTypes.Role) ?? "Aluno");
        return Ok(await marketplace.ListarAsync(usuarioId, tipo, cancellationToken));
    }

    [HttpPost("{id:int}/visibilidade")]
    public async Task<ActionResult> AlterarVisibilidade(int id, [FromBody] AlterarVisibilidadeRequest request)
    {
        try
        {
            var atorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await visibilidade.PublicarErroCatalogoAsync(id, request.Nivel, request.EscolaId, request.TurmaIds, atorId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    [HttpPost("{id:int}/arquivar")]
    public async Task<ActionResult> Arquivar(int id)
    {
        try { await visibilidade.ArquivarErroCatalogoAsync(id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    [HttpPost("{id:int}/restaurar")]
    public async Task<ActionResult> Restaurar(int id)
    {
        try { await visibilidade.RestaurarErroCatalogoAsync(id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }
}
