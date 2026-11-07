using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Aplicacao.Services;
using Ditado.Dominio.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExcelDataReader;
using System.Globalization;
using System.Text;
using System.Data;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsuariosController : ControllerBase
{
	private readonly UsuarioService _usuarioService;

	public UsuariosController(UsuarioService usuarioService)
	{
		_usuarioService = usuarioService;
	}

	/// <summary>
	/// Cria um novo usu�rio (Admin apenas)
	/// </summary>
	/// <remarks>
	/// Apenas Administradores podem criar usu�rios diretamente no sistema.
	/// O tipo de usu�rio (Aluno, Professor, Administrador) � definido no momento da cria��o.
	/// 
	/// Para auto-cadastro de alunos, use o endpoint `/solicitar-acesso`.
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     POST /api/usuarios
	///     {
	///        "nome": "Jo�o Silva",
	///        "login": "joao.silva@escola.com",
	///        "senha": "senha123",
	///        "matricula": "2024001",
	///        "tipo": "Aluno"
	///     }
	/// </remarks>
	/// <param name="request">Dados do usu�rio a ser criado</param>
	/// <returns>Usu�rio criado</returns>
	/// <response code="201">Usu�rio criado com sucesso</response>
	/// <response code="400">Dados inv�lidos (email duplicado, tipo inv�lido, etc.)</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Administrador)</response>
	[HttpPost]
	[Authorize(Roles = "Administrador")]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<UsuarioResponse>> CriarUsuario([FromBody] CriarUsuarioRequest request)
	{
		try
		{
			var usuario = await _usuarioService.CriarUsuarioAsync(request);
			return CreatedAtAction(nameof(ObterUsuario), new { id = usuario.Id }, usuario);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>
	/// Solicita acesso ao sistema (auto-cadastro)
	/// </summary>
	/// <remarks>
	/// Endpoint p�blico para que novos usu�rios solicitem acesso.
	/// O usu�rio � criado com tipo "AcessoSolicitado" e status inativo.
	/// 
	/// Ap�s a solicita��o:
	/// 1. Aguardar aprova��o de um Administrador ou Professor
	/// 2. Ap�s aprova��o, o login ser� liberado com o tipo apropriado (Aluno, Professor)
	/// 
	/// N�o � poss�vel fazer login enquanto o acesso n�o for aprovado.
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     POST /api/usuarios/solicitar-acesso
	///     {
	///        "nome": "Maria Santos",
	///        "login": "maria.santos@escola.com",
	///        "senha": "senhaSegura123",
	///        "matricula": "2024002"
	///     }
	/// </remarks>
	/// <param name="request">Dados do usu�rio solicitante</param>
	/// <returns>Confirma��o de solicita��o enviada</returns>
	/// <response code="200">Solicita��o enviada com sucesso</response>
	/// <response code="400">Dados inv�lidos (email j� cadastrado, etc.)</response>
	[HttpPost("solicitar-acesso")]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<UsuarioResponse>> SolicitarAcesso([FromBody] CriarUsuarioRequest request)
	{
		try
		{
			var usuario = await _usuarioService.SolicitarAcessoAsync(request);
			return Ok(new
			{
				mensagem = "Solicita��o de acesso enviada com sucesso! Aguarde a aprova��o de um administrador ou professor.",
				usuario
			});
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>Solicita cadastro de professor.</summary>
	/// <remarks>
	/// Cria uma solicitação de acesso para professor vinculada a uma escola existente
	/// ou a uma nova escola, conforme <c>OpcaoEscola</c>. A senha é armazenada apenas
	/// como hash e nunca é retornada na resposta.
	/// </remarks>
	/// <param name="request">Dados do professor e opção de escola.</param>
	/// <response code="200">Solicitação criada com sucesso.</response>
	/// <response code="400">Dados inválidos, login duplicado ou escola inexistente.</response>
	[HttpPost("cadastro-professor")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<UsuarioResponse>> CadastroProfessor([FromBody] CadastroProfessorRequest request)
	{
		try { return Ok(await _usuarioService.CadastrarProfessorAsync(request)); }
		catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
	}

	/// <summary>Solicita pré-cadastro de aluno sem definir PIN.</summary>
	/// <remarks>Gera um código de cadastro. O PIN é criado pelo próprio aluno no primeiro acesso.</remarks>
	/// <param name="request">Nome e matrícula do aluno.</param>
	/// <response code="200">Pré-cadastro criado e código de cadastro retornado.</response>
	/// <response code="400">Matrícula inválida ou já cadastrada.</response>
	[HttpPost("solicitar-acesso-aluno")]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<UsuarioResponse>> SolicitarAcessoAluno([FromBody] SolicitarAcessoAlunoRequest request)
	{
		try { return Ok(new { mensagem = "Solicitação enviada com sucesso! Aguarde a aprovação.", usuario = await _usuarioService.SolicitarAcessoAlunoAsync(request) }); }
		catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
	}

	/// <summary>
	/// Lista solicita��es de acesso pendentes
	/// </summary>
	/// <remarks>
	/// Retorna todos os usu�rios com tipo "AcessoSolicitado" e status inativo,
	/// aguardando aprova��o de um Administrador ou Professor.
	/// 
	/// Usado para:
	/// - Administradores e Professores revisarem novos cadastros
	/// - Aprovar ou rejeitar solicita��es de acesso
	/// </remarks>
	/// <returns>Lista de solicita��es pendentes</returns>
	/// <response code="200">Lista retornada com sucesso</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
	[HttpGet("solicitacoes-pendentes")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(typeof(List<UsuarioResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<List<UsuarioResponse>>> ListarSolicitacoesPendentes()
	{
		var solicitacoes = await _usuarioService.ListarSolicitacoesPendentesAsync();
		return Ok(solicitacoes);
	}

	/// <summary>
	/// Aprova uma solicita��o de acesso
	/// </summary>
	/// <remarks>
	/// Permite que Administradores e Professores aprovem solicita��es de acesso pendentes.
	/// 
	/// **Regras de aprova��o:**
	/// - **Administrador**: Pode aprovar para qualquer tipo (Aluno, Professor, Administrador)
	/// - **Professor**: Pode aprovar apenas como Aluno ou Professor (n�o pode criar Administradores)
	/// 
	/// Ap�s aprova��o:
	/// - Usu�rio fica ativo
	/// - Tipo � alterado conforme especificado
	/// - Usu�rio pode fazer login no sistema
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     POST /api/usuarios/5/aprovar-acesso
	///     {
	///        "novoTipo": "Aluno"
	///     }
	/// </remarks>
	/// <param name="id">ID do usu�rio a ser aprovado</param>
	/// <param name="request">Novo tipo de usu�rio ap�s aprova��o</param>
	/// <returns>Usu�rio aprovado</returns>
	/// <response code="200">Acesso aprovado com sucesso</response>
	/// <response code="400">Dados inv�lidos (tipo n�o permitido, usu�rio j� aprovado, etc.)</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Admin e Professor)</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpPost("{id}/aprovar-acesso")]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<UsuarioResponse>> AprovarAcesso(int id, [FromBody] AprovarAcessoRequest request)
	{
		try
		{
			var tipoUsuarioLogado = User.FindFirst(ClaimTypes.Role)?.Value;
			var tipoEnum = Enum.Parse<TipoUsuario>(tipoUsuarioLogado ?? "Aluno");

			var usuario = await _usuarioService.AprovarAcessoAsync(id, request, tipoEnum);

			if (usuario == null)
				return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

			return Ok(new
			{
				mensagem = "Acesso aprovado com sucesso!",
				usuario
			});
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	/// <summary>
	/// Faz login no sistema
	/// </summary>
	/// <remarks>
	/// Autentica um usu�rio e retorna um token JWT para acesso aos endpoints protegidos.
	/// 
	/// **Requisitos para login:**
	/// - Email e senha v�lidos
	/// - Usu�rio com status ativo (n�o pode estar bloqueado)
	/// - Acesso j� aprovado (n�o pode ter tipo "AcessoSolicitado")
	/// 
	/// O token JWT deve ser enviado no header `Authorization: Bearer {token}` nas requisi��es subsequentes.
	/// 
	/// Exemplo de requisi��o:
	/// 
	///     POST /api/usuarios/login
	///     {
	///        "login": "usuario@escola.com",
	///        "senha": "senha123"
	///     }
	///     
	/// Exemplo de resposta:
	/// 
	///     {
	///        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	///        "usuario": {
	///           "id": 1,
	///           "nome": "Jo�o Silva",
	///           "login": "usuario@escola.com",
	///           "tipo": "Aluno"
	///        }
	///     }
	/// </remarks>
	/// <param name="request">Credenciais de login</param>
	/// <returns>Token JWT e dados do usu�rio</returns>
	/// <response code="200">Login realizado com sucesso</response>
	/// <response code="401">Credenciais inv�lidas, acesso n�o aprovado ou conta inativa</response>
	[HttpPost("login")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
	{
		try
		{
			var resultado = await _usuarioService.LoginAsync(request);

			if (resultado == null)
				return Unauthorized(new { mensagem = "Login ou senha inv�lidos." });

			return Ok(resultado);
		}
		catch (InvalidOperationException ex)
		{
			return Unauthorized(new { mensagem = ex.Message });
		}
	}

	/// <summary>Autentica aluno por matrícula ou código de cadastro e PIN.</summary>
	/// <remarks>O PIN nunca é definido ou retornado por professor ou administrador.</remarks>
	/// <param name="request">Identificador do aluno e PIN criado no primeiro acesso.</param>
	/// <response code="200">Token JWT e dados do aluno.</response>
	/// <response code="401">Identificador/PIN inválido ou PIN ainda não configurado.</response>
	[HttpPost("login-aluno")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<LoginResponse>> LoginAluno([FromBody] LoginAlunoRequest request)
	{
		try
		{
			var response = await _usuarioService.LoginAlunoAsync(request);
			return response is null ? Unauthorized(new { mensagem = "Matrícula ou código de cadastro, ou PIN, inválido." }) : Ok(response);
		}
		catch (InvalidOperationException ex) { return Unauthorized(new { mensagem = ex.Message }); }
	}

	/// <summary>Configura o PIN do aluno no primeiro acesso.</summary>
	/// <remarks>Disponível apenas para pré-cadastros ainda sem PIN configurado.</remarks>
	/// <param name="request">Matrícula ou código de cadastro e novo PIN.</param>
	/// <response code="200">PIN configurado.</response>
	/// <response code="400">Identificador inválido ou PIN já configurado.</response>
	[HttpPost("primeiro-acesso-aluno")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<UsuarioResponse>> PrimeiroAcessoAluno([FromBody] PrimeiroAcessoAlunoRequest request)
	{
		var usuario = await _usuarioService.ConfigurarPrimeiroPinAlunoAsync(request);
		return usuario is null ? BadRequest(new { mensagem = "Você ainda não foi pré-cadastrado na plataforma. Fale com seu professor ou administrador da sua escola." }) : Ok(usuario);
	}

	/// <summary>Verifica se a matrícula ou código pertence a um aluno pré-cadastrado sem PIN.</summary>
	/// <remarks>Esta etapa não cria sessão nem altera dados; apenas libera a tela seguinte para criação do PIN.</remarks>
	/// <param name="request">Matrícula ou código de entrada do aluno.</param>
	/// <response code="200">Identificador elegível para criação do PIN.</response>
	/// <response code="400">Aluno não pré-cadastrado ou PIN já configurado.</response>
	[HttpPost("primeiro-acesso-aluno/verificar")]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult> VerificarPrimeiroAcessoAluno([FromBody] VerificarPrimeiroAcessoAlunoRequest request)
	{
		var existe = await _usuarioService.VerificarPrimeiroAcessoAlunoAsync(request);
		return existe ? Ok(new { podeConfigurarPin = true }) : BadRequest(new { mensagem = "Você ainda não foi pré-cadastrado na plataforma. Fale com seu professor ou administrador da sua escola." });
	}

	/// <summary>Cadastra manualmente um aluno em uma turma.</summary>
	/// <remarks>Gera código escola-turma-chamada; o PIN será criado pelo aluno no primeiro acesso.</remarks>
	/// <param name="request">Nome, número de chamada, matrícula opcional e turma.</param>
	/// <response code="200">Aluno cadastrado e convite retornado.</response>
	/// <response code="400">Dados inválidos, turma inativa ou aluno duplicado.</response>
	/// <response code="401">Token ausente ou inválido.</response>
	/// <response code="403">Usuário sem permissão para a turma.</response>
	[HttpPost("alunos-manual")]
	[Authorize(Roles = "Professor,Administrador")]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<UsuarioResponse>> CadastrarAlunoManual([FromBody] CadastrarAlunoManualRequest request)
	{
		var atorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		try { return Ok(await _usuarioService.CadastrarAlunoManualAsync(request, atorId)); }
		catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
	}

	/// <summary>Pré-visualiza uma planilha CSV ou XLS de alunos sem gravar dados.</summary>
	[HttpPost("importar-alunos/preview")]
	[Authorize(Roles = "Professor,Administrador")]
	[RequestSizeLimit(10_000_000)]
	[ProducesResponseType(typeof(IReadOnlyList<ImportarAlunoLinhaResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<IReadOnlyList<ImportarAlunoLinhaResponse>>> ImportarAlunosPreview(IFormFile arquivo)
	{
		if (arquivo is null || arquivo.Length == 0) return BadRequest(new { mensagem = "Arquivo vazio." });
		var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
		if (extensao is not ".csv" and not ".xls" and not ".xlsx") return BadRequest(new { mensagem = "Use arquivo .csv, .xls ou .xlsx." });
		if (arquivo.Length > 10_000_000) return BadRequest(new { mensagem = "Arquivo excede 10 MB." });
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		await using var stream = arquivo.OpenReadStream();
		using var reader = extensao == ".csv"
			? ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration { AutodetectSeparators = new[] { ',', ';', '\t' }, FallbackEncoding = Encoding.UTF8 })
			: ExcelReaderFactory.CreateReader(stream);
		var data = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true } });
		var table = data.Tables.Count == 0 ? null : data.Tables[0];
		if (table is null) return BadRequest(new { mensagem = "Nenhuma planilha encontrada." });
		if (table.Rows.Count > 1000) return BadRequest(new { mensagem = "A planilha pode conter no máximo 1000 linhas." });
		var rows = new List<ImportarAlunoLinhaResponse>();
		for (var i = 0; i < table.Rows.Count; i++)
		{
			var row = table.Rows[i];
			var nome = Valor(row, table, "nome");
			var chamadaTexto = Valor(row, table, "número da chamada", "numero da chamada", "chamada");
			var matricula = Valor(row, table, "matrícula", "matricula");
			var turma = Valor(row, table, "turma");
			var pendencias = new List<string>();
			if (string.IsNullOrWhiteSpace(nome)) pendencias.Add("Nome obrigatório");
			if (!int.TryParse(chamadaTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var chamada) || chamada < 1) pendencias.Add("Número da chamada inválido");
			rows.Add(new ImportarAlunoLinhaResponse { Linha = i + 2, Nome = nome, NumeroChamada = chamada > 0 ? chamada : null, Matricula = matricula, Turma = turma, Valida = pendencias.Count == 0, Pendencias = pendencias.ToArray() });
		}
		return Ok(rows);
	}

	private static string? Valor(DataRow row, DataTable table, params string[] nomes)
	{
		foreach (DataColumn col in table.Columns)
		{
			var normalizado = col.ColumnName.Trim().ToLowerInvariant();
			if (nomes.Any(n => normalizado == n)) return row[col]?.ToString()?.Trim();
		}
		return null;
	}

	[HttpPost("sessao/revogar")]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult> RevogarSessaoAtual()
	{
		var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
		if (!Guid.TryParse(jti, out var sessaoId))
			return BadRequest(new { mensagem = "Sessão inválida." });

		var revogada = await _usuarioService.RevogarSessaoAsync(sessaoId, usuarioId, "Revogação solicitada pelo usuário.");
		return revogada ? NoContent() : BadRequest(new { mensagem = "Sessão já revogada ou inexistente." });
	}

	[HttpGet("contexto-escolar")]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<IReadOnlyList<ContextoEscolarResponse>>> ListarContextoEscolar()
	{
		var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		var contextos = await _usuarioService.ListarContextosEscolaresAsync(usuarioId);
		var escolaHeader = Request.Headers["X-Escola-Id"].FirstOrDefault();

		if (string.IsNullOrWhiteSpace(escolaHeader))
			return Ok(contextos);

		if (!int.TryParse(escolaHeader, out var escolaId))
		{
			await _usuarioService.RegistrarAuditoriaAsync(usuarioId, null, "SelecionarContextoEscolar", "Escola", "Negado", "X-Escola-Id inválido.");
			return BadRequest(new { mensagem = "X-Escola-Id deve ser um identificador numérico." });
		}

		var autorizado = await _usuarioService.PossuiAcessoEscolarAsync(usuarioId, escolaId);
		await _usuarioService.RegistrarAuditoriaAsync(usuarioId, escolaId, "SelecionarContextoEscolar", "Escola", autorizado ? "Permitido" : "Negado", autorizado ? "Vínculo escolar ativo." : "Usuário sem vínculo escolar ativo.");

		if (!autorizado)
			return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Usuário não possui vínculo ativo com a escola informada." });

		return Ok(contextos.Where(c => c.EscolaId == escolaId).ToList());
	}

	/// <summary>
	/// Obt�m um usu�rio por ID
	/// </summary>
	/// <remarks>
	/// Retorna os dados completos de um usu�rio espec�fico.
	/// Qualquer usu�rio autenticado pode acessar este endpoint.
	/// </remarks>
	/// <param name="id">ID do usu�rio</param>
	/// <returns>Dados do usu�rio</returns>
	/// <response code="200">Usu�rio encontrado</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpGet("{id}")]
	[Authorize]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<UsuarioResponse>> ObterUsuario(int id)
	{
		var usuario = await _usuarioService.ObterPorIdAsync(id);

		if (usuario == null)
			return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

		return Ok(usuario);
	}

	/// <summary>
	/// Lista todos os usu�rios
	/// </summary>
	/// <remarks>
	/// Retorna uma lista completa de todos os usu�rios cadastrados no sistema.
	/// Administradores e Professores podem acessar este endpoint.
	/// </remarks>
	/// <returns>Lista de usu�rios</returns>
	/// <response code="200">Lista retornada com sucesso</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Administrador e Professor)</response>
	[HttpGet]
	[Authorize(Roles = "Administrador,Professor")]
	[ProducesResponseType(typeof(List<UsuarioResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<List<UsuarioResponse>>> ListarUsuarios()
	{
		var usuarios = await _usuarioService.ListarUsuariosAsync();
		return Ok(usuarios);
	}

	/// <summary>
	/// Atualiza um usu�rio
	/// </summary>
	/// <remarks>
	/// Permite atualizar dados de um usu�rio com diferentes n�veis de permiss�o:
	/// 
	/// **Edi��o pr�pria (qualquer usu�rio autenticado):**
	/// - Nome, Matr�cula, Senha (com senha atual)
	/// - N�O pode alterar pr�prio Tipo ou Status Ativo
	/// 
	/// **Edi��o de outros usu�rios:**
	/// 
	/// **Alterar Status Ativo:**
	/// - Aluno: N�o pode alterar ningu�m
	/// - Professor: Pode ativar/desativar apenas Alunos
	/// - Administrador: Pode ativar/desativar qualquer um
	/// 
	/// **Alterar Tipo:**
	/// - Aluno: N�o pode alterar ningu�m
	/// - Professor: Pode promover Aluno > Professor (n�o pode criar Administrador nem reduzir tipos)
	/// - Administrador: Pode promover ou reduzir qualquer tipo (exceto para "AcessoSolicitado")
	/// 
	/// Exemplo de requisi��o (edi��o pr�pria):
	/// 
	///     PUT /api/usuarios/5
	///     {
	///        "nome": "Jo�o Silva Santos",
	///        "senhaAtual": "senha123",
	///        "senhaNova": "novaSenha456"
	///     }
	///     
	/// Exemplo de requisi��o (Admin alterando tipo):
	/// 
	///     PUT /api/usuarios/10
	///     {
	///        "tipo": "Professor",
	///        "ativo": true
	///     }
	/// </remarks>
	/// <param name="id">ID do usu�rio</param>
	/// <param name="request">Dados a serem atualizados</param>
	/// <returns>Usu�rio atualizado</returns>
	/// <response code="200">Usu�rio atualizado com sucesso</response>
	/// <response code="400">Dados inv�lidos (senha incorreta, permiss�o negada, etc.)</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpPut("{id}")]
	[Authorize]
	[ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<UsuarioResponse>> AtualizarUsuario(int id, [FromBody] AtualizarUsuarioRequest request)
	{
		try
		{
			// Obter ID e Tipo do usu�rio logado
			var usuarioLogadoId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
			var tipoUsuarioLogado = Enum.Parse<TipoUsuario>(User.FindFirst(ClaimTypes.Role)?.Value ?? "Aluno");

			var usuario = await _usuarioService.AtualizarUsuarioAsync(id, request, usuarioLogadoId, tipoUsuarioLogado);

			if (usuario == null)
				return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

			return Ok(usuario);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { mensagem = ex.Message });
		}
	}

	[HttpPatch("{id}/administrador-geral")]
	[Authorize(Roles = "Administrador")]
	public async Task<ActionResult> AlterarAdministradorGeral(int id, [FromBody] AlterarAdministradorGeralRequest request)
	{
		if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atorId)) return Unauthorized();
		try
		{
			await _usuarioService.AlterarAdministradorGeralAsync(id, request.Habilitar, atorId, request.Motivo, request.CorrelacaoId);
			return NoContent();
		}
		catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
	}

	/// <summary>
	/// Bloqueia um usu�rio
	/// </summary>
	/// <remarks>
	/// Define o status do usu�rio como inativo, impedindo login no sistema.
	/// Apenas Administradores podem bloquear usu�rios.
	/// 
	/// O usu�rio bloqueado n�o ser� exclu�do, apenas ficar� inativo.
	/// Use `/desbloquear` para reativar o acesso.
	/// </remarks>
	/// <param name="id">ID do usu�rio</param>
	/// <response code="204">Usu�rio bloqueado com sucesso</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Administrador)</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpPatch("{id}/bloquear")]
	[Authorize(Roles = "Administrador")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> BloquearUsuario(int id)
	{
		var sucesso = await _usuarioService.BloquearUsuarioAsync(id);

		if (!sucesso)
			return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

		return NoContent();
	}

	/// <summary>
	/// Desbloqueia um usu�rio
	/// </summary>
	/// <remarks>
	/// Reativa um usu�rio bloqueado, permitindo login novamente.
	/// Apenas Administradores podem desbloquear usu�rios.
	/// </remarks>
	/// <param name="id">ID do usu�rio</param>
	/// <response code="204">Usu�rio desbloqueado com sucesso</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Administrador)</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpPatch("{id}/desbloquear")]
	[Authorize(Roles = "Administrador")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> DesbloquearUsuario(int id)
	{
		var sucesso = await _usuarioService.DesbloquearUsuarioAsync(id);

		if (!sucesso)
			return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

		return NoContent();
	}

	/// <summary>
	/// Exclui um usu�rio permanentemente
	/// </summary>
	/// <remarks>
	/// Remove um usu�rio do sistema de forma irrevers�vel.
	/// Apenas Administradores podem excluir usu�rios.
	/// 
	/// **ATEN��O**: Esta opera��o � permanente e n�o pode ser desfeita!
	/// Considere usar `/bloquear` ao inv�s de excluir para manter o hist�rico.
	/// </remarks>
	/// <param name="id">ID do usu�rio</param>
	/// <response code="204">Usu�rio exclu�do com sucesso</response>
	/// <response code="401">N�o autenticado</response>
	/// <response code="403">Sem permiss�o (apenas Administrador)</response>
	/// <response code="404">Usu�rio n�o encontrado</response>
	[HttpDelete("{id}")]
	[Authorize(Roles = "Administrador")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> DeletarUsuario(int id)
	{
		var sucesso = await _usuarioService.DeletarUsuarioAsync(id);

		if (!sucesso)
			return NotFound(new { mensagem = "Usu�rio n�o encontrado." });

		return NoContent();
	}
}
