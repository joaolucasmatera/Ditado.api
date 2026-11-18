using System.Security.Claims;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/Alunos/tentativas")]
[Authorize(Roles = "Aluno")]
public sealed class ResultadoAlunoController(AlunoService alunoService) : ControllerBase
{
    /// <summary>Consulta a correção de uma tentativa do aluno autenticado.</summary>
    /// <remarks>Inclui nota, gabarito, respostas originais, estado da análise LLM e origem de cada achado. Requer vínculo ativo na escola da tentativa quando houver escola registrada.</remarks>
    /// <param name="tentativaId">Identificador retornado na submissão ou no histórico.</param>
    /// <response code="200">Resultado da tentativa.</response>
    /// <response code="401">Token ausente ou inválido.</response>
    /// <response code="403">Perfil diferente de Aluno.</response>
    /// <response code="404">Tentativa indisponível ao aluno.</response>
    [HttpGet("{tentativaId:int}/resultado")]
    [ProducesResponseType(typeof(ResultadoDitadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultadoDitadoResponse>> ObterMeuResultado(int tentativaId)
    {
        var alunoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resultado = await alunoService.ObterMeuResultadoAsync(alunoId, tentativaId);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}
