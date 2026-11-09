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
        try
        {
            var publicado = await marketplace.PublicarAsync(request, autorId, cancellationToken);
            return CreatedAtAction(nameof(ObterRecibo), new { id = publicado.Id }, publicado);
        }
        catch (InvalidOperationException ex) { return Forbid(ex.Message); }
    }

    /// <summary>Atualiza a definição de um erro do catálogo.</summary>
    /// <remarks>
    /// Professores só podem atualizar erros de sua autoria. Administradores da Escola
    /// precisam de vínculo ativo com a escola do autor/publicação; Administrador Geral
    /// possui capability global. Após a primeira associação a um ditado, a definição fica imutável.
    /// </remarks>
    /// <param name="id">ID do erro de catálogo.</param>
    /// <param name="request">Campos editáveis da definição.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <response code="200">Erro atualizado.</response>
    /// <response code="400">Erro arquivado, já utilizado ou dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Ator sem autorização no escopo atual.</response>
    /// <response code="404">Erro não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ErroPublicadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ErroPublicadoResponse>> Atualizar(int id, [FromBody] PublicarErroRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atorId))
            return Unauthorized();
        var tipo = Enum.Parse<Dominio.Enums.TipoUsuario>(User.FindFirstValue(ClaimTypes.Role) ?? nameof(Dominio.Enums.TipoUsuario.Aluno));
        try
        {
            var atualizado = await marketplace.AtualizarAsync(request, id, atorId, tipo, cancellationToken);
            return atualizado is null ? NotFound(new { mensagem = "Erro de catálogo não encontrado." }) : Ok(atualizado);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
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

    /// <summary>Lista erros do catálogo no escopo do usuário autenticado.</summary>
    /// <param name="estado">Estado da lista: <c>ativo</c> ou <c>arquivado</c>.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <response code="200">Erros visíveis no escopo atual, incluindo autoria e nível de privacidade.</response>
    /// <response code="401">Não autenticado.</response>
    [HttpGet]
    [Authorize(Roles = "Administrador,Professor,Aluno")]
    public async Task<ActionResult<List<ErroPublicadoResponse>>> Listar([FromQuery] string estado = "ativo", CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
        var tipo = Enum.Parse<Dominio.Enums.TipoUsuario>(User.FindFirstValue(ClaimTypes.Role) ?? "Aluno");
        return Ok(await marketplace.ListarAsync(usuarioId, tipo, estado, cancellationToken));
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
