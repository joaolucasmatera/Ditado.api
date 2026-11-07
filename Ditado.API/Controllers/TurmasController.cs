using System.Security.Claims;
using Ditado.Aplicacao.DTOs.Turmas;
using Ditado.Aplicacao.Services;
using Ditado.Dominio.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TurmasController : ControllerBase
{
    private readonly TurmaService _turmaService;

    public TurmasController(TurmaService turmaService)
    {
        _turmaService = turmaService;
    }

    /// <summary>
    /// Cria uma nova turma
    /// </summary>
    /// <remarks>
    /// Apenas Administradores e Professores podem criar turmas.
    /// 
    /// Exemplo de requisi��o:
    /// 
    ///     POST /api/turmas
    ///     {
    ///        "nome": "5� Ano A",
    ///        "serie": 5,
    ///        "anoLetivo": 2024,
    ///        "semestre": "1� Semestre",
    ///        "descricao": "Turma da manh�",
    ///        "professorResponsavelId": 1,
    ///        "alunosIds": [2, 3, 4]
    ///     }
    /// </remarks>
    /// <param name="request">Dados da turma a ser criada</param>
    /// <returns>Turma criada com sucesso</returns>
    /// <response code="201">Turma criada com sucesso</response>
    /// <response code="400">Dados inv�lidos (professor inv�lido, alunos inv�lidos, etc.)</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
    [HttpPost]
    [Authorize(Roles = "Administrador,Professor")]
    [ProducesResponseType(typeof(TurmaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TurmaResponse>> CriarTurma([FromBody] CriarTurmaRequest request)
    {
        try
        {
            var usuarioLogadoId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var tipoUsuarioLogado = Enum.Parse<TipoUsuario>(User.FindFirst(ClaimTypes.Role)?.Value ?? "Aluno");
            var turma = await _turmaService.CriarTurmaAsync(request, usuarioLogadoId, tipoUsuarioLogado);
            return CreatedAtAction(nameof(ObterTurma), new { id = turma.Id }, turma);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Obt�m uma turma por ID
    /// </summary>
    /// <remarks>
    /// Retorna os dados completos de uma turma espec�fica, incluindo:
    /// - Informa��es b�sicas (nome, s�rie, ano letivo)
    /// - Professor respons�vel
    /// - Lista completa de alunos matriculados
    /// </remarks>
    /// <param name="id">ID da turma</param>
    /// <returns>Dados da turma</returns>
    /// <response code="200">Turma encontrada</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="404">Turma n�o encontrada</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TurmaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TurmaResponse>> ObterTurma(int id)
    {
        var turma = await _turmaService.ObterPorIdAsync(id);

        if (turma == null)
            return NotFound(new { mensagem = "Turma n�o encontrada." });

        return Ok(turma);
    }

    /// <summary>
    /// Lista todas as turmas
    /// </summary>
    /// <remarks>
    /// Retorna uma lista de turmas, com op��o de filtrar apenas turmas ativas.
    /// Por padr�o, retorna apenas turmas ativas (Ativo = true).
    /// </remarks>
    /// <param name="apenasAtivas">Filtrar apenas turmas ativas (padr�o: true)</param>
    /// <returns>Lista de turmas</returns>
    /// <response code="200">Lista de turmas retornada com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TurmaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TurmaResponse>>> ListarTurmas([FromQuery] bool? apenasAtivas = true)
    {
        var turmas = await _turmaService.ListarTurmasAsync(apenasAtivas);
        return Ok(turmas);
    }

    /// <summary>
    /// Lista turmas de um professor espec�fico
    /// </summary>
    /// <remarks>
    /// Retorna apenas as turmas onde o professor especificado � o respons�vel.
    /// Apenas Administradores e Professores podem acessar este endpoint.
    /// </remarks>
    /// <param name="professorId">ID do professor</param>
    /// <returns>Lista de turmas do professor</returns>
    /// <response code="200">Lista de turmas retornada com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
    [HttpGet("professor/{professorId}")]
    [Authorize(Roles = "Administrador,Professor")]
    [ProducesResponseType(typeof(List<TurmaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TurmaResponse>>> ListarTurmasPorProfessor(int professorId)
    {
        var turmas = await _turmaService.ListarTurmasPorProfessorAsync(professorId);
        return Ok(turmas);
    }

    /// <summary>
    /// Lista turmas de um aluno espec�fico
    /// </summary>
    /// <remarks>
    /// Retorna todas as turmas onde o aluno especificado est� matriculado.
    /// 
    /// **Permiss�es:**
    /// - **Aluno**: Pode consultar apenas suas pr�prias turmas
    /// - **Professor/Admin**: Podem consultar turmas de qualquer aluno
    /// 
    /// Retorna apenas turmas ativas.
    /// </remarks>
    /// <param name="alunoId">ID do aluno</param>
    /// <returns>Lista de turmas do aluno</returns>
    /// <response code="200">Lista de turmas retornada com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (aluno tentando ver turmas de outro aluno)</response>
    [HttpGet("aluno/{alunoId}")]
    [Authorize(Roles = "Administrador,Professor,Aluno")]
    [ProducesResponseType(typeof(List<TurmaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TurmaResponse>>> ListarTurmasPorAluno(int alunoId)
    {
        // Validar permiss�o: Aluno s� pode ver suas pr�prias turmas
        var usuarioLogadoId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var tipoUsuarioLogado = Enum.Parse<TipoUsuario>(User.FindFirst(ClaimTypes.Role)?.Value ?? "Aluno");

        if (tipoUsuarioLogado == TipoUsuario.Aluno && usuarioLogadoId != alunoId)
        {
            return StatusCode(403, new { mensagem = "Voc� s� pode visualizar suas pr�prias turmas." });
        }

        var turmas = await _turmaService.ListarTurmasPorAlunoAsync(alunoId);
        return Ok(turmas);
    }

    /// <summary>
    /// Atualiza uma turma existente
    /// </summary>
    /// <remarks>
    /// Permite atualizar qualquer campo da turma:
    /// - Nome, s�rie, ano letivo, semestre, descri��o
    /// - Professor respons�vel
    /// - Lista de alunos (substitui completamente a lista atual)
    /// - Status ativo/inativo
    /// 
    /// Apenas Administradores e Professores podem atualizar turmas.
    /// 
    /// Exemplo de requisi��o para adicionar alunos:
    /// 
    ///     PUT /api/turmas/1
    ///     {
    ///        "alunosIds": [2, 3, 4, 5]
    ///     }
    ///     
    /// Para remover todos os alunos:
    /// 
    ///     PUT /api/turmas/1
    ///     {
    ///        "alunosIds": []
    ///     }
    /// </remarks>
    /// <param name="id">ID da turma</param>
    /// <param name="request">Dados a serem atualizados (apenas campos preenchidos ser�o alterados)</param>
    /// <returns>Turma atualizada</returns>
    /// <response code="200">Turma atualizada com sucesso</response>
    /// <response code="400">Dados inv�lidos</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
    /// <response code="404">Turma n�o encontrada</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Professor")]
    [ProducesResponseType(typeof(TurmaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TurmaResponse>> AtualizarTurma(int id, [FromBody] AtualizarTurmaRequest request)
    {
        try
        {
            var turma = await _turmaService.AtualizarTurmaAsync(id, request);

            if (turma == null)
                return NotFound(new { mensagem = "Turma n�o encontrada." });

            return Ok(turma);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Exclui uma turma
    /// </summary>
    /// <remarks>
    /// Remove permanentemente uma turma do sistema.
    /// 
    /// **Regras de permiss�o:**
    /// - **Administrador**: Pode excluir qualquer turma
    /// - **Professor**: Pode excluir apenas turmas onde ele � o respons�vel
    /// 
    ///  **ATEN��O**: Esta opera��o � irrevers�vel! 
    /// Considere desativar a turma ao inv�s de exclu�-la (usando PUT com "ativo": false).
    /// </remarks>
    /// <param name="id">ID da turma</param>
    /// <response code="204">Turma exclu�da com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (professor tentando excluir turma de outro professor)</response>
    /// <response code="404">Turma n�o encontrada</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExcluirTurma(int id)
    {
        try
        {
            // Obter ID e Tipo do usu�rio logado
            var usuarioLogadoId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var tipoUsuarioLogado = Enum.Parse<TipoUsuario>(User.FindFirst(ClaimTypes.Role)?.Value ?? "Aluno");

            var sucesso = await _turmaService.ExcluirTurmaAsync(id, usuarioLogadoId, tipoUsuarioLogado);

            if (!sucesso)
                return NotFound(new { mensagem = "Turma n�o encontrada." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { mensagem = ex.Message });
        }
    }

	/// <summary>
	/// Adiciona um aluno � turma
	/// </summary>
	/// <remarks>
	/// Matricula um aluno espec�fico em uma turma.
	/// Apenas Administradores e Professores podem adicionar alunos.
	/// 
	/// **Valida��es:**
	/// - Turma deve existir e estar ativa
	/// - ID fornecido deve ser de um usu�rio com tipo "Aluno"
	/// - Aluno n�o pode j� estar matriculado na turma
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     POST /api/turmas/5/alunos/10
	///     
	/// (Adiciona aluno ID 10 � turma ID 5)
	/// </remarks>
	/// <param name="turmaId">ID da turma</param>
	/// <param name="alunoId">ID do aluno a ser adicionado</param>
	/// <returns>Turma atualizada com novo aluno</returns>
	/// <response code="200">Aluno adicionado com sucesso</response>
	/// <response code="400">Aluno inv�lido ou j� matriculado</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
	/// <response code="404">Turma n�o encontrada</response>
	[HttpPost("{turmaId}/alunos/{alunoId}")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(typeof(TurmaResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TurmaResponse>> AdicionarAluno(int turmaId, int alunoId)
	{
		try
		{
			var turma = await _turmaService.AdicionarAlunoAsync(turmaId, alunoId);

			if (turma == null)
				return NotFound(new { mensagem = "Turma n�o encontrada." });

			return Ok(turma);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>
	/// Remove um aluno da turma
	/// </summary>
	/// <remarks>
	/// Desmatricula um aluno espec�fico de uma turma.
	/// Apenas Administradores e Professores podem remover alunos.
	/// 
	/// **Valida��es:**
	/// - Turma deve existir
	/// - Aluno deve estar matriculado na turma
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     DELETE /api/turmas/5/alunos/10
	///     
	/// (Remove aluno ID 10 da turma ID 5)
	/// </remarks>
	/// <param name="turmaId">ID da turma</param>
	/// <param name="alunoId">ID do aluno a ser removido</param>
	/// <returns>Turma atualizada sem o aluno</returns>
	/// <response code="200">Aluno removido com sucesso</response>
	/// <response code="400">Aluno n�o est� na turma</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
	/// <response code="404">Turma n�o encontrada</response>
	[HttpDelete("{turmaId}/alunos/{alunoId}")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(typeof(TurmaResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TurmaResponse>> RemoverAluno(int turmaId, int alunoId)
	{
		try
		{
			var turma = await _turmaService.RemoverAlunoAsync(turmaId, alunoId);

			if (turma == null)
				return NotFound(new { mensagem = "Turma n�o encontrada." });

			return Ok(turma);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>
	/// Atribui um ditado a uma turma
	/// </summary>
	/// <remarks>
	/// Apenas Professores e Administradores podem atribuir ditados.
	/// 
	/// **Exemplo de requisi��o:**
	/// 
	///     POST /api/turmas/5/ditados
	///     {
	///        "ditadoId": 10,
	///        "dataLimite": "2024-12-31T23:59:59"
	///     }
	/// </remarks>
	/// <param name="id">ID da turma</param>
	/// <param name="request">Dados da atribui��o</param>
	/// <response code="204">Ditado atribu�do com sucesso</response>
	/// <response code="400">Dados inv�lidos (ditado j� atribu�do, etc.)</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o</response>
	/// <response code="404">Turma n�o encontrada</response>
	[HttpPost("{id}/ditados")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> AtribuirDitado(int id, [FromBody] AtribuirDitadoRequest request)
	{
		try
		{
			var usuarioId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
			var tipoUsuario = Enum.Parse<Dominio.Enums.TipoUsuario>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Aluno");
			var sucesso = await _turmaService.AtribuirDitadoAsync(id, request, usuarioId, tipoUsuario);

			if (!sucesso)
				return NotFound(new { mensagem = "Turma n�o encontrada." });

			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>
	/// Atualiza a data limite de um ditado atribu�do
	/// </summary>
	/// <param name="id">ID da turma</param>
	/// <param name="ditadoId">ID do ditado</param>
	/// <param name="request">Nova data limite</param>
	/// <response code="204">Data limite atualizada</response>
	/// <response code="404">Atribui��o n�o encontrada</response>
	[HttpPut("{id}/ditados/{ditadoId}")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> AtualizarAtribuicao(int id, int ditadoId, [FromBody] AtualizarAtribuicaoRequest request)
	{
	var sucesso = await _turmaService.AtualizarAtribuicaoAsync(id, ditadoId, request);

	if (!sucesso)
		return NotFound(new { mensagem = "Atribui��o n�o encontrada." });

	return NoContent();
}

	/// <summary>
	/// Remove atribui��o de ditado de uma turma
	/// </summary>
	/// <remarks>
	/// **ATEN��O:** Alunos ainda poder�o ver suas tentativas anteriores mesmo ap�s remover a atribui��o.
	/// </remarks>
	/// <param name="id">ID da turma</param>
	/// <param name="ditadoId">ID do ditado</param>
	/// <response code="204">Atribui��o removida</response>
	/// <response code="404">Atribui��o n�o encontrada</response>
	[HttpDelete("{id}/ditados/{ditadoId}")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> RemoverAtribuicao(int id, int ditadoId)
	{
	var sucesso = await _turmaService.RemoverAtribuicaoAsync(id, ditadoId);

	if (!sucesso)
		return NotFound(new { mensagem = "Atribui��o n�o encontrada." });

	return NoContent();
}
}
