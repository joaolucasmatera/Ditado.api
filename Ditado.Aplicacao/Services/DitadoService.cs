using Ditado.Aplicacao.DTOs;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Extensions;
using Ditado.Dominio.Services;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Ditado.Aplicacao.Llm;

namespace Ditado.Aplicacao.Services;

public class DitadoService
{
	private readonly DitadoDbContext _context;
	private readonly CategoriaService _categoriaService;
	private readonly LlmOptions _llmOptions;

	public DitadoService(DitadoDbContext context, CategoriaService categoriaService, IOptions<LlmOptions>? llmOptions = null)
	{
		_context = context;
		_categoriaService = categoriaService;
		_llmOptions = llmOptions?.Value ?? new LlmOptions();
	}

	public async Task<DitadoResponse> CriarDitadoAsync(CriarDitadoRequest request, int autorId)
	{
		var segmentos = ParsearTextoComLacunas(request.TextoComMarcacoes);
		var audioDados = ExtrairAudioDeBase64(request.AudioBase64);
		if (request.ErroCatalogoIds is not { Count: > 0 })
			throw new InvalidOperationException("Selecione pelo menos um erro do catálogo.");
		var (escolaId, professorId) = await ResolverContextoSelecaoAsync(autorId, null);
		var errosSelecionados = await ObterNovosErrosAsync(request.ErroCatalogoIds, escolaId, professorId);
		var agora = DateTime.UtcNow;

		var ditado = new Dominio.Entidades.Ditado
		{
			Titulo = request.Titulo,
			Descricao = request.Descricao,
			AudioLeitura = audioDados,
			DataCriacao = agora,
			Ativo = true,
			AutorId = autorId,
			Segmentos = segmentos,
			EscolaSelecaoErrosId = escolaId
		};

		if (request.CategoriaIds != null && request.CategoriaIds.Any())
		{
			await _categoriaService.ValidarCategoriasAsync(request.CategoriaIds);
			foreach (var categoriaId in request.CategoriaIds.Distinct())
				ditado.DitadoCategorias.Add(new DitadoCategoria { CategoriaId = categoriaId, DataAssociacao = agora });
		}

		foreach (var erro in errosSelecionados)
		{
			erro.UtilizadoEm ??= agora;
			ditado.ErrosCatalogoSelecionados.Add(new DitadoErroCatalogo { ErroCatalogo = erro });
		}
		_context.Ditados.Add(ditado);
		await _context.SaveChangesAsync();
		return await ObterDitadoComCategoriasAsync(ditado.Id);
	}

	public async Task<DitadoParaRealizarResponse?> ObterDitadoParaRealizarAsync(int id, int? usuarioId = null, TipoUsuario? tipoUsuario = null)
	{
		var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral);
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.Include(d => d.Publicacoes).ThenInclude(p => p.TurmasAlvo)
			.FirstOrDefaultAsync(d => d.Id == id && d.Ativo && (usuarioId == null || administradorGlobal || d.AutorId == usuarioId ||
				d.Publicacoes.Any(p => p.ChaveVigente != null && (p.Nivel == NivelVisibilidade.PublicoGeral ||
				(p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && _context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) ||
				(p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))));

		if (ditado == null)
			return null;

		var audioBase64 = $"data:{Dominio.Entidades.Ditado.AudioMimeTypePadrao};base64,{Convert.ToBase64String(ditado.AudioLeitura)}";

		return new DitadoParaRealizarResponse
		{
			Id = ditado.Id,
			Titulo = ditado.Titulo,
			AudioBase64 = audioBase64,
			Segmentos = ditado.Segmentos.OrderBy(s => s.Ordem).Select(s => new SegmentoParaRealizarDto
			{
				Ordem = s.Ordem,
				Tipo = s.Tipo.ToString(),
				Conteudo = s.Tipo == TipoSegmento.Texto ? s.Conteudo : null,
				SegmentoId = s.Tipo == TipoSegmento.Lacuna ? s.Id : null
			}).ToList()
		};
	}

	/// <summary>
	/// Obtém os metadados, áudio e segmentos completos de um ditado ativo para visualização por staff.
	/// </summary>
	public async Task<DitadoCompletoDto?> ObterDitadoCompletoPorIdAsync(int id)
	{
		return await ObterDitadoCompletoPorIdAsync(id, null, null);
	}

	public async Task<DitadoCompletoDto?> ObterDitadoCompletoPorIdAsync(int id, int? usuarioId, TipoUsuario? tipoUsuario)
	{
		var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral);
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.Include(d => d.Autor)
			.Include(d => d.DitadoCategorias)
				.ThenInclude(dc => dc.Categoria)
			.Include(d => d.ErrosCatalogoSelecionados).ThenInclude(e => e.ErroCatalogo)
			.FirstOrDefaultAsync(d => d.Id == id && d.Ativo && (usuarioId == null || administradorGlobal || d.AutorId == usuarioId || d.Publicacoes.Any(p => p.ChaveVigente != null && (p.Nivel == NivelVisibilidade.PublicoGeral || (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && _context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) || (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))));

		if (ditado == null)
			return null;

		var podeVerSelecao = !usuarioId.HasValue || await PodeAdministrarDitadoAsync(ditado, usuarioId.Value, tipoUsuario);
		return new DitadoCompletoDto
		{
			Id = ditado.Id,
			Titulo = ditado.Titulo,
			Descricao = ditado.Descricao,
			DataCriacao = ditado.DataCriacao,
			AutorId = ditado.AutorId,
			AutorNome = ditado.Autor?.Nome,
			ErrosCatalogo = podeVerSelecao ? MapearSelecao(ditado) : [],
			PossuiRespostas = podeVerSelecao && await _context.RespostaDitados.AnyAsync(r => r.DitadoId == id),
			Categorias = ditado.DitadoCategorias
				.Select(associacao => new CategoriaSimplificadaDto
				{
					Id = associacao.Categoria.Id,
					Nome = associacao.Categoria.Nome
				})
				.ToList(),
			AudioBase64 = $"data:{Dominio.Entidades.Ditado.AudioMimeTypePadrao};base64,{Convert.ToBase64String(ditado.AudioLeitura)}",
			Segmentos = ditado.Segmentos
				.OrderBy(segmento => segmento.Ordem)
				.Select(segmento => new SegmentoCompletoDto
				{
					Id = segmento.Id,
					Ordem = segmento.Ordem,
					Tipo = segmento.Tipo.ToString(),
					Conteudo = segmento.Conteudo
				})
				.ToList()
		};
	}

	public async Task<ResultadoDitadoResponse?> SubmeterRespostaAsync(int ditadoId, SubmeterRespostaRequest request, int alunoId)
	{
		await using var transacao = await SelecaoErrosCatalogo.BloquearDitadoAsync(_context, ditadoId);
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.Include(d => d.ErrosCatalogoSelecionados).ThenInclude(e => e.ErroCatalogo)
			.FirstOrDefaultAsync(d => d.Id == ditadoId && d.Ativo);

		if (ditado == null)
			return null;

		var usaCatalogo = ditado.ErrosCatalogoSelecionados.Count > 0;
		var aplicacao = await _context.AplicacoesDitados
			.Where(a => a.DitadoId == ditadoId && a.Ativa && a.Participacoes.Any(p => p.AlunoId == alunoId && !p.RevogadaEm.HasValue))
			.Where(a => !usaCatalogo || (a.Escola.Ativa && a.Turma.Ativo && a.Turma.EscolaId == a.EscolaId &&
				a.Turma.Alunos.Any(u => u.Id == alunoId && u.Ativo) &&
				_context.VinculosEscolares.Any(v => v.UsuarioId == alunoId && v.EscolaId == a.EscolaId && v.Ativo)))
			.Include(a => a.ErrosSelecionados).ThenInclude(e => e.ErroCatalogo)
			.OrderByDescending(a => a.DataAtribuicao)
			.FirstOrDefaultAsync();

		if (usaCatalogo && aplicacao == null)
		{
			var ator = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == alunoId && u.Ativo);
			if (ator == null || (ator.Tipo == TipoUsuario.Aluno
				? !await _context.TurmaDitados.AnyAsync(t => t.DitadoId == ditadoId && t.Turma.Ativo &&
					t.Turma.Alunos.Any(a => a.Id == alunoId) && (t.Turma.EscolaId == null ||
						_context.VinculosEscolares.Any(v => v.UsuarioId == alunoId && v.EscolaId == t.Turma.EscolaId && v.Ativo && v.Escola.Ativa)))
				: !await PodeAdministrarDitadoAsync(ditado, alunoId, ator.Tipo)))
				return null;
		}

		var definicoes = aplicacao == null
			? ditado.ErrosCatalogoSelecionados.Select(e => e.ErroCatalogo)
			: aplicacao.ErrosSelecionados.Select(e => e.ErroCatalogo);
		var candidatosLlm = _llmOptions.PodeEnfileirar && usaCatalogo && aplicacao?.EscolaId != null
			? definicoes.Where(e => !e.RegraClassificacao.HasValue).ToArray()
			: [];
		var idsCandidatos = candidatosLlm.Select(e => e.Id).ToArray();
		var versoesCandidatos = candidatosLlm.Length == 0
			? new Dictionary<int, int>()
			: await _context.PublicacoesErrosCatalogo
				.Where(p => idsCandidatos.Contains(p.ErroCatalogoId))
				.GroupBy(p => p.ErroCatalogoId)
				.Select(g => new { Id = g.Key, Versao = g.Max(p => p.Versao) })
				.ToDictionaryAsync(p => p.Id, p => p.Versao);
		var regrasSelecionadas = usaCatalogo
			? definicoes.Where(e => e.RegraClassificacao.HasValue).ToDictionary(e => e.RegraClassificacao!.Value)
			: new Dictionary<TipoErro, ErroCatalogo>();
		var regras = regrasSelecionadas.Keys.ToHashSet();
		var respostaDitado = new RespostaDitado
		{
			DitadoId = ditadoId,
			AlunoId = alunoId,
			AplicacaoDitadoId = aplicacao?.Id,
			EscolaId = aplicacao?.EscolaId,
			DataRealizacao = DateTime.UtcNow
		};

		var detalhes = new List<DetalheRespostaDto>();
		int acertos = 0;
		int totalLacunas = 0;

		var respostas = request.Respostas.GroupBy(r => r.SegmentoId).ToDictionary(g => g.Key, g => g.First().Resposta);
		foreach (var segmento in ditado.Segmentos.Where(s => s.Tipo == TipoSegmento.Lacuna).OrderBy(s => s.Ordem))
		{
			var presente = respostas.TryGetValue(segmento.Id, out var resposta);
			resposta ??= string.Empty;
			totalLacunas++;
			var correto = presente && NormalizarTextoParaComparacao(resposta) == NormalizarTextoParaComparacao(segmento.Conteudo);
			if (correto) acertos++;

			var tiposErro = correto ? Array.Empty<TipoErro>() :
				ClassificadorTiposErro.IdentificarTiposErro(resposta, segmento.Conteudo, regras);

			var respostaSegmento = new RespostaSegmento
			{
				SegmentoId = segmento.Id,
				RespostaFornecida = resposta,
				RespostaAusente = !presente,
				RespostaEsperada = segmento.Conteudo,
				Correto = correto,
				EstadoClassificacao = correto ? EstadoClassificacao.NaoSeAplica :
					(tiposErro.Count > 0 ? EstadoClassificacao.Classificado : EstadoClassificacao.NaoClassificado),
				MetodoClassificacao = tiposErro.Count > 0 ? MetodoClassificacao.Codigo : null,
				TiposErro = tiposErro.Select(tipo => new RespostaSegmentoTipoErro
				{
					TipoErro = tipo,
					ErroCatalogoId = regrasSelecionadas.GetValueOrDefault(tipo)?.Id
				}).ToList()
			};

			respostaDitado.RespostasSegmentos.Add(respostaSegmento);
			if (candidatosLlm.Length > 0)
			{
				respostaSegmento.AnaliseLlm = new LlmAnaliseTarefa
				{
					RespostaSegmento = respostaSegmento,
					PromptVersao = _llmOptions.VersaoAtiva,
					Candidatos = candidatosLlm.Select(erro => new LlmAnaliseCandidato
					{
						ErroCatalogoId = erro.Id,
						Codigo = erro.Codigo,
						Nome = erro.Nome,
						Descricao = erro.Descricao,
						ExemploEsperado = erro.ExemploEsperado,
						ExemploResposta = erro.ExemploResposta,
						ExemplosAdicionaisJson = erro.ExemplosAdicionaisJson,
						ObservacaoPedagogica = erro.ObservacaoPedagogica,
						Versao = versoesCandidatos.GetValueOrDefault(erro.Id)
					}).ToList()
				};
			}

			detalhes.Add(new DetalheRespostaDto
			{
				SegmentoId = segmento.Id,
				RespostaFornecida = resposta,
				RespostaAusente = !presente,
				EstadoAnaliseLlm = respostaSegmento.AnaliseLlm?.Estado.ToString(),
				RespostaEsperada = segmento.Conteudo,
				Correto = correto,
				EstadoClassificacao = respostaSegmento.EstadoClassificacao.ToString(),
				MetodoClassificacao = respostaSegmento.MetodoClassificacao?.ToString(),
				TiposErro = tiposErro.Select(tipo => tipo.ObterDescricao()).ToList(),
				Classificacoes = tiposErro.Where(regrasSelecionadas.ContainsKey).Select(tipo =>
				{
					var erro = regrasSelecionadas[tipo];
					return new ClassificacaoCatalogoDto(erro.Id, erro.Codigo, erro.Nome, (int)tipo);
				}).ToList()
			});
		}

		respostaDitado.Nota = totalLacunas > 0
			? decimal.Round((decimal)acertos / totalLacunas * 100, 2, MidpointRounding.AwayFromZero)
			: 0;

		_context.RespostaDitados.Add(respostaDitado);
		await _context.SaveChangesAsync();
		if (transacao != null)
			await transacao.CommitAsync();

		return new ResultadoDitadoResponse
		{
			RespostaDitadoId = respostaDitado.Id,
			Nota = respostaDitado.Nota,
			TotalLacunas = totalLacunas,
			Acertos = acertos,
			Erros = totalLacunas - acertos,
			Detalhes = detalhes
		};
	}

	public async Task<List<DitadoResponse>> ListarDitadosAsync(int? usuarioId = null, TipoUsuario? tipoUsuario = null)
	{
		var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral);
		return await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.ThenInclude(dc => dc.Categoria)
			.Include(d => d.Autor)
			.Where(d => d.Ativo && (usuarioId == null || administradorGlobal || d.AutorId == usuarioId ||
				d.Publicacoes.Any(p => p.ChaveVigente != null && (p.Nivel == NivelVisibilidade.PublicoGeral ||
				(p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId != null && _context.VinculosEscolares.Any(v => v.UsuarioId == usuarioId && v.EscolaId == p.EscolaId && v.Ativo)) ||
				(p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.Turma.ProfessorResponsavelId == usuarioId || t.Turma.Alunos.Any(a => a.Id == usuarioId)))))))
			.OrderByDescending(d => d.DataCriacao)
			.Select(d => new DitadoResponse
			{
				Id = d.Id,
				Titulo = d.Titulo,
				Descricao = d.Descricao,
				DataCriacao = d.DataCriacao,
				AutorId = d.AutorId,
				AutorNome = d.Autor != null ? d.Autor.Nome : null, 
				NivelVisibilidade = d.Publicacoes.Where(p => p.ChaveVigente != null).OrderByDescending(p => p.Versao).Select(p => p.Nivel.ToString()).FirstOrDefault(),
				Categorias = d.DitadoCategorias
					.Select(dc => new CategoriaSimplificadaDto
					{
						Id = dc.Categoria.Id,
						Nome = dc.Categoria.Nome
					})
					.ToList()
			})
			.ToListAsync();
	}

	private async Task<DitadoResponse> ObterDitadoComCategoriasAsync(int ditadoId)
	{
		var ditado = await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.ThenInclude(dc => dc.Categoria)
			.Include(d => d.Autor)
			.Include(d => d.Publicacoes)
			.Include(d => d.ErrosCatalogoSelecionados).ThenInclude(e => e.ErroCatalogo)
			.FirstOrDefaultAsync(d => d.Id == ditadoId);

		if (ditado == null)
			throw new InvalidOperationException("Ditado n�o encontrado.");

		return new DitadoResponse
		{
			Id = ditado.Id,
			Titulo = ditado.Titulo,
			Descricao = ditado.Descricao,
			DataCriacao = ditado.DataCriacao,
			AutorId = ditado.AutorId,
			AutorNome = ditado.Autor?.Nome,
			ErrosCatalogo = MapearSelecao(ditado),
			NivelVisibilidade = ditado.Publicacoes.Where(p => p.ChaveVigente != null).OrderByDescending(p => p.Versao).Select(p => p.Nivel.ToString()).FirstOrDefault(),
			Categorias = ditado.DitadoCategorias
				.Select(dc => new CategoriaSimplificadaDto
				{
					Id = dc.Categoria.Id,
					Nome = dc.Categoria.Nome
				})
				.ToList()
		};
	}

	public async Task<DitadoResponse?> AtualizarCategoriasAsync(int ditadoId, List<int> categoriaIds, int? usuarioId = null, TipoUsuario? tipoUsuario = null)
	{
		var ditado = await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.FirstOrDefaultAsync(d => d.Id == ditadoId);

		if (ditado == null)
			return null;
		if (usuarioId.HasValue && !await PodeAdministrarDitadoAsync(ditado, usuarioId.Value, tipoUsuario))
			throw new InvalidOperationException("Usuário sem escopo para este ditado.");

		// Validar categorias
		await _categoriaService.ValidarCategoriasAsync(categoriaIds);

		// Remover associa��es antigas
		_context.DitadoCategorias.RemoveRange(ditado.DitadoCategorias);

		// Adicionar novas associa��es
		foreach (var categoriaId in categoriaIds.Distinct())
		{
			_context.DitadoCategorias.Add(new DitadoCategoria
			{
				DitadoId = ditado.Id,
				CategoriaId = categoriaId,
				DataAssociacao = DateTime.UtcNow
			});
		}

		await _context.SaveChangesAsync();

		return await ObterDitadoComCategoriasAsync(ditado.Id);
	}

	public async Task<DitadoResponse?> AtualizarDitadoAsync(int id, AtualizarDitadoRequest request, int? usuarioId = null, TipoUsuario? tipoUsuario = null)
	{
		await using var transacao = request.ErroCatalogoIds != null
			? await SelecaoErrosCatalogo.BloquearDitadoAsync(_context, id)
			: null;
		var ditado = await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.Include(d => d.ErrosCatalogoSelecionados)
			.FirstOrDefaultAsync(d => d.Id == id);

		if (ditado == null)
			return null;
		if (usuarioId.HasValue && !await PodeAdministrarDitadoAsync(ditado, usuarioId.Value, tipoUsuario))
			throw new InvalidOperationException("Usuário sem escopo para este ditado.");

		if (request.CategoriaIds is { Count: > 0 })
			await _categoriaService.ValidarCategoriasAsync(request.CategoriaIds);
		if (request.ErroCatalogoIds != null)
			await AtualizarSelecaoAsync(ditado, request.ErroCatalogoIds, usuarioId ?? ditado.AutorId ?? 0);

		if (!string.IsNullOrWhiteSpace(request.Titulo))
			ditado.Titulo = request.Titulo;

		// Atualizar descri��o se fornecido (permite limpar)
		if (request.Descricao != null)
			ditado.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao;

		// Atualizar status ativo se fornecido
		if (request.Ativo.HasValue)
			ditado.Ativo = request.Ativo.Value;

		// Atualizar categorias se fornecido (permite 0 categorias)
		if (request.CategoriaIds != null)
		{
			// Remover associa��es antigas
			_context.DitadoCategorias.RemoveRange(ditado.DitadoCategorias);

			// Adicionar novas associa��es (pode ser lista vazia)
			foreach (var categoriaId in request.CategoriaIds.Distinct())
			{
				_context.DitadoCategorias.Add(new DitadoCategoria
				{
					DitadoId = ditado.Id,
					CategoriaId = categoriaId,
					DataAssociacao = DateTime.UtcNow
				});
			}
		}

		await _context.SaveChangesAsync();
		if (transacao != null)
			await transacao.CommitAsync();

		return await ObterDitadoComCategoriasAsync(ditado.Id);
	}

	private async Task AtualizarSelecaoAsync(Dominio.Entidades.Ditado ditado, List<int> erroIds, int atorId)
	{
		if (await _context.RespostaDitados.AnyAsync(r => r.DitadoId == ditado.Id))
			throw new InvalidOperationException("Não é possível alterar a seleção depois da primeira resposta.");
		if (erroIds.Count == 0)
			throw new InvalidOperationException("Selecione pelo menos um erro do catálogo.");

		var atorDoDitado = ditado.AutorId;
		var escolaOrigem = ditado.EscolaSelecaoErrosId ?? await _context.PublicacoesDitados
			.Where(p => p.DitadoId == ditado.Id && p.ChaveVigente != null)
			.Select(p => p.EscolaId).FirstOrDefaultAsync()
			?? await _context.VinculosEscolares.Where(v => v.UsuarioId == atorDoDitado && v.Ativo && v.Escola.Ativa)
				.Select(v => (int?)v.EscolaId).FirstOrDefaultAsync();
		var (escolaId, professorId) = await ResolverContextoSelecaoAsync(atorId, escolaOrigem);
		var ids = erroIds.ToHashSet();
		var adicionados = ids.Except(ditado.ErrosCatalogoSelecionados.Select(e => e.ErroCatalogoId)).ToArray();
		var novos = await ObterNovosErrosAsync(adicionados, escolaId, professorId);

		var aplicacoes = await _context.AplicacoesDitados.Where(a => a.DitadoId == ditado.Id && a.Ativa)
			.Include(a => a.ErrosSelecionados).ToListAsync();
		var turmasLegadas = await _context.TurmaDitados.Where(t => t.DitadoId == ditado.Id)
			.Select(t => new { t.TurmaId, t.Turma.EscolaId }).ToListAsync();
		foreach (var aplicacao in aplicacoes)
			await SelecaoErrosCatalogo.ValidarDestinoAsync(_context, aplicacao.EscolaId, aplicacao.TurmaId, adicionados);
		foreach (var turma in turmasLegadas)
			await SelecaoErrosCatalogo.ValidarDestinoAsync(_context, turma.EscolaId, turma.TurmaId, adicionados);

		ditado.EscolaSelecaoErrosId = escolaId;
		foreach (var anterior in ditado.ErrosCatalogoSelecionados.Where(e => !ids.Contains(e.ErroCatalogoId)).ToArray())
		{
			ditado.ErrosCatalogoSelecionados.Remove(anterior);
			_context.DitadosErrosCatalogo.Remove(anterior);
		}
		foreach (var erro in novos)
		{
			erro.UtilizadoEm ??= DateTime.UtcNow;
			ditado.ErrosCatalogoSelecionados.Add(new DitadoErroCatalogo { ErroCatalogo = erro });
		}
		foreach (var aplicacao in aplicacoes)
		{
			_context.AplicacoesErrosCatalogo.RemoveRange(aplicacao.ErrosSelecionados.Where(e => !ids.Contains(e.ErroCatalogoId)).ToArray());
			foreach (var erro in novos)
				aplicacao.ErrosSelecionados.Add(SelecaoErrosCatalogo.RegistrarAplicacao(erro));
		}
	}

	private async Task<(int? EscolaId, int? ProfessorId)> ResolverContextoSelecaoAsync(int atorId, int? escolaId)
	{
		var ator = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo);
		if (ator == null || ator.Tipo is not (TipoUsuario.Administrador or TipoUsuario.Professor))
			throw new UnauthorizedAccessException("Perfil sem permissão para selecionar erros.");

		var escolas = _context.VinculosEscolares.Where(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa);
		escolaId ??= await escolas.Select(v => (int?)v.EscolaId).FirstOrDefaultAsync();
		if (!(ator.Tipo == TipoUsuario.Administrador && ator.AdministradorGeral) &&
			(!escolaId.HasValue || !await escolas.AnyAsync(v => v.EscolaId == escolaId)))
			throw new UnauthorizedAccessException("É necessário vínculo ativo na escola da seleção.");

		return (escolaId, ator.Tipo == TipoUsuario.Professor ? atorId : null);
	}

	private async Task<List<ErroCatalogo>> ObterNovosErrosAsync(IReadOnlyCollection<int> erroIds, int? escolaId, int? professorId)
	{
		if (erroIds.Count == 0)
			return [];
		var ids = erroIds.Distinct().ToArray();
		var erros = await SelecaoErrosCatalogo.Disponiveis(_context, escolaId, professorId: professorId)
			.Where(e => ids.Contains(e.Id)).ToListAsync();
		if (erros.Count != ids.Length)
			throw new InvalidOperationException("A seleção contém erros inexistentes, arquivados ou fora do escopo.");
		return erros;
	}

	private static List<ErroSelecionadoDto> MapearSelecao(Dominio.Entidades.Ditado ditado) => ditado.ErrosCatalogoSelecionados
		.Select(e => e.ErroCatalogo).OrderBy(e => e.Id)
		.Select(e => new ErroSelecionadoDto(e.Id, e.Codigo, e.Nome, e.Ativo, e.RegraClassificacao.HasValue)).ToList();

	private async Task<bool> PodeAdministrarDitadoAsync(Dominio.Entidades.Ditado ditado, int usuarioId, TipoUsuario? tipoUsuario)
	{
		if (ditado.EscolaSelecaoErrosId.HasValue)
		{
			if (tipoUsuario == TipoUsuario.Administrador &&
				await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral))
				return true;
			return (ditado.AutorId == usuarioId || tipoUsuario == TipoUsuario.Administrador) &&
				await _context.VinculosEscolares.AnyAsync(v => v.UsuarioId == usuarioId &&
					v.EscolaId == ditado.EscolaSelecaoErrosId && v.Ativo && v.Escola.Ativa && v.Usuario.Ativo);
		}
		if (ditado.AutorId == usuarioId) return true;
		if (tipoUsuario != TipoUsuario.Administrador) return false;
		if (await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral)) return true;
		return await _context.VinculosEscolares.AnyAsync(v => v.UsuarioId == usuarioId && v.Ativo && v.Escola.Ativa &&
			((_context.PublicacoesDitados.Any(p => p.DitadoId == ditado.Id && p.EscolaId == v.EscolaId)) ||
			 (ditado.AutorId.HasValue && _context.VinculosEscolares.Any(autorVinculo => autorVinculo.UsuarioId == ditado.AutorId.Value && autorVinculo.EscolaId == v.EscolaId && autorVinculo.Ativo && autorVinculo.Escola.Ativa))));
	}

	public async Task<bool> DeletarDitadoAsync(int ditadoId, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
	{
		var ditado = await _context.Ditados.FindAsync(ditadoId);

		if (ditado == null)
			return false;

		if (!await PodeAdministrarDitadoAsync(ditado, usuarioLogadoId, tipoUsuarioLogado))
			throw new InvalidOperationException("Apenas o autor do ditado ou um administrador podem exclu�-lo.");

		_context.Ditados.Remove(ditado);
		await _context.SaveChangesAsync();

		return true;
	}

	private List<DitadoSegmento> ParsearTextoComLacunas(string texto)
	{
		var segmentos = new List<DitadoSegmento>();
		var regex = new Regex(@"\[([^\]]+)\]");
		int ordem = 1;
		int ultimoIndice = 0;

		foreach (Match match in regex.Matches(texto))
		{
			// Extrai o conte�do dentro dos colchetes (pode ter espa�os/pontua��o)
			var conteudoBruto = match.Groups[1].Value;

			// Separa palavra limpa de prefixo/sufixo (espa�os e pontua��o)
			var (prefixo, palavraLimpa, sufixo) = ExtrairPalavraComContexto(conteudoBruto);

			// Adiciona texto ANTES da lacuna (incluindo o que estava antes dos colchetes + prefixo)
			var textoAntes = texto.Substring(ultimoIndice, match.Index - ultimoIndice) + prefixo;

			if (!string.IsNullOrEmpty(textoAntes))
			{
				segmentos.Add(new DitadoSegmento
				{
					Ordem = ordem++,
					Tipo = TipoSegmento.Texto,
					Conteudo = textoAntes
				});
			}

			// Adiciona a LACUNA com apenas a palavra limpa (preserva mai�scula/min�scula original)
			segmentos.Add(new DitadoSegmento
			{
				Ordem = ordem++,
				Tipo = TipoSegmento.Lacuna,
				Conteudo = palavraLimpa
			});

			// Atualiza �ndice
			ultimoIndice = match.Index + match.Length;

			// Se tem sufixo, ele ser� o in�cio do pr�ximo segmento de texto
			if (!string.IsNullOrEmpty(sufixo))
			{
				// Guarda o sufixo para concatenar com o pr�ximo texto
				var proximoTextoInicio = ultimoIndice;
				var proximoTextoFim = ultimoIndice < texto.Length ?
					(regex.Match(texto, ultimoIndice).Success ? regex.Match(texto, ultimoIndice).Index : texto.Length)
					: texto.Length;

				var proximoTexto = proximoTextoInicio < proximoTextoFim ?
					texto.Substring(proximoTextoInicio, proximoTextoFim - proximoTextoInicio) : string.Empty;

				var textoComSufixo = sufixo + proximoTexto;

				if (!string.IsNullOrEmpty(textoComSufixo) && proximoTextoInicio < texto.Length)
				{
					segmentos.Add(new DitadoSegmento
					{
						Ordem = ordem++,
						Tipo = TipoSegmento.Texto,
						Conteudo = textoComSufixo
					});

					// Atualiza �ndice para pular o texto j� processado
					ultimoIndice = proximoTextoFim;
				}
				else if (!string.IsNullOrEmpty(sufixo))
				{
					// Apenas sufixo, sem texto adicional
					segmentos.Add(new DitadoSegmento
					{
						Ordem = ordem++,
						Tipo = TipoSegmento.Texto,
						Conteudo = sufixo
					});
				}
			}
		}

		// Adiciona texto restante ap�s a �ltima lacuna
		if (ultimoIndice < texto.Length)
		{
			segmentos.Add(new DitadoSegmento
			{
				Ordem = ordem,
				Tipo = TipoSegmento.Texto,
				Conteudo = texto.Substring(ultimoIndice)
			});
		}

		return segmentos;
	}

	/// <summary>
	/// Extrai a palavra limpa e separa prefixo/sufixo (espa�os e pontua��o).
	/// Preserva a capitaliza��o original da palavra.
	/// Exemplo: ", Gato. " > (", ", "Gato", ". ")
	/// </summary>
	private (string prefixo, string palavra, string sufixo) ExtrairPalavraComContexto(string conteudo)
	{
		// Regex que captura: (prefixo) (palavra) (sufixo)
		// Prefixo: espa�os e pontua��o (incluindo ponto) no in�cio
		// Palavra: letras (com acentos), n�meros, ap�strofo e h�fen
		// Sufixo: espa�os e pontua��o (incluindo ponto) no fim
		var match = Regex.Match(
			conteudo,
			@"^([\s,\.;:!?\-�'""��]*)([a-zA-Z����������������������������������������������'\-]+)([\s,\.;:!?\-�'""��]*)$"
		);

		if (match.Success)
		{
			return (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
		}

		// Fallback: Se n�o conseguiu fazer match, tenta pelo menos separar espa�os
		var trimmed = conteudo.Trim();
		var prefixoEspacos = conteudo.Substring(0, conteudo.IndexOf(trimmed[0]));
		var sufixoEspacos = conteudo.Substring(conteudo.LastIndexOf(trimmed[^1]) + 1);

		return (prefixoEspacos, trimmed, sufixoEspacos);
	}

	private byte[] ExtrairAudioDeBase64(string audioBase64)
	{
		var base64Data = audioBase64.Contains(",") ? audioBase64.Split(',')[1] : audioBase64;
		return Convert.FromBase64String(base64Data);
	}

	private string NormalizarTextoParaComparacao(string texto)
	{
		// Capitalization is part of objective grading; only outer whitespace is ignored.
		return texto.Trim();
	}

}
