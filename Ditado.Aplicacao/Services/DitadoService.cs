using Ditado.Aplicacao.DTOs;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Extensions;
using Ditado.Dominio.Services;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Ditado.Aplicacao.Services;

public class DitadoService
{
	private readonly DitadoDbContext _context;
	private readonly CategoriaService _categoriaService;

	public DitadoService(DitadoDbContext context, CategoriaService categoriaService)
	{
		_context = context;
		_categoriaService = categoriaService;
	}

	public async Task<DitadoResponse> CriarDitadoAsync(CriarDitadoRequest request, int autorId)
	{
		var segmentos = ParsearTextoComLacunas(request.TextoComMarcacoes);
		var audioDados = ExtrairAudioDeBase64(request.AudioBase64);

		var ditado = new Dominio.Entidades.Ditado
		{
			Titulo = request.Titulo,
			Descricao = request.Descricao,
			AudioLeitura = audioDados,
			DataCriacao = DateTime.UtcNow,
			Ativo = true,
			AutorId = autorId,
			Segmentos = segmentos
		};

		_context.Ditados.Add(ditado);
		await _context.SaveChangesAsync();

		// ADICIONAR: Associar categorias ao ditado
		if (request.CategoriaIds != null && request.CategoriaIds.Any())
		{
			await _categoriaService.ValidarCategoriasAsync(request.CategoriaIds);

			foreach (var categoriaId in request.CategoriaIds.Distinct())
			{
				_context.DitadoCategorias.Add(new DitadoCategoria
				{
					DitadoId = ditado.Id,
					CategoriaId = categoriaId,
					DataAssociacao = DateTime.UtcNow
				});
			}

			await _context.SaveChangesAsync();
		}

		return await ObterDitadoComCategoriasAsync(ditado.Id);
	}

	public async Task<DitadoParaRealizarResponse?> ObterDitadoParaRealizarAsync(int id, int? usuarioId = null, TipoUsuario? tipoUsuario = null)
	{
		var administradorGlobal = usuarioId.HasValue && tipoUsuario == TipoUsuario.Administrador && await _context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Ativo && u.AdministradorGeral);
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.Include(d => d.Publicacoes).ThenInclude(p => p.TurmasAlvo)
			.FirstOrDefaultAsync(d => d.Id == id && d.Ativo && (usuarioId == null || administradorGlobal ||
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
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.Include(d => d.Autor)
			.Include(d => d.DitadoCategorias)
				.ThenInclude(dc => dc.Categoria)
			.FirstOrDefaultAsync(d => d.Id == id && d.Ativo);

		if (ditado == null)
			return null;

		return new DitadoCompletoDto
		{
			Id = ditado.Id,
			Titulo = ditado.Titulo,
			Descricao = ditado.Descricao,
			DataCriacao = ditado.DataCriacao,
			AutorId = ditado.AutorId,
			AutorNome = ditado.Autor?.Nome,
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
		var ditado = await _context.Ditados
			.Include(d => d.Segmentos)
			.FirstOrDefaultAsync(d => d.Id == ditadoId && d.Ativo);

		if (ditado == null)
			return null;

		var aplicacao = await _context.AplicacoesDitados
			.Where(a => a.DitadoId == ditadoId && a.Ativa && a.Participacoes.Any(p => p.AlunoId == alunoId && !p.RevogadaEm.HasValue))
			.OrderByDescending(a => a.DataAtribuicao)
			.FirstOrDefaultAsync();

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

		foreach (var resposta in request.Respostas)
		{
			var segmento = ditado.Segmentos.FirstOrDefault(s => s.Id == resposta.SegmentoId);
			if (segmento == null || segmento.Tipo != TipoSegmento.Lacuna)
				continue;

			totalLacunas++;
			var correto = NormalizarTextoParaComparacao(resposta.Resposta) == NormalizarTextoParaComparacao(segmento.Conteudo);
			if (correto) acertos++;

			var tiposErro = correto ? Array.Empty<TipoErro>() : IdentificarTiposErro(resposta.Resposta, segmento.Conteudo);

			var respostaSegmento = new RespostaSegmento
			{
				SegmentoId = segmento.Id,
				RespostaFornecida = resposta.Resposta,
				RespostaEsperada = segmento.Conteudo,
				Correto = correto,
				EstadoClassificacao = correto ? EstadoClassificacao.NaoSeAplica :
					(tiposErro.Count > 0 ? EstadoClassificacao.Classificado : EstadoClassificacao.NaoClassificado),
				MetodoClassificacao = tiposErro.Count > 0 ? MetodoClassificacao.Codigo : null,
				TiposErro = tiposErro.Select(tipo => new RespostaSegmentoTipoErro { TipoErro = tipo }).ToList()
			};

			respostaDitado.RespostasSegmentos.Add(respostaSegmento);

			detalhes.Add(new DetalheRespostaDto
			{
				SegmentoId = segmento.Id,
				RespostaFornecida = resposta.Resposta,
				RespostaEsperada = segmento.Conteudo,
				Correto = correto,
				EstadoClassificacao = (correto ? EstadoClassificacao.NaoSeAplica :
					(tiposErro.Count > 0 ? EstadoClassificacao.Classificado : EstadoClassificacao.NaoClassificado)).ToString(),
				MetodoClassificacao = tiposErro.Count > 0 ? MetodoClassificacao.Codigo.ToString() : null,
				TiposErro = tiposErro.Select(tipo => tipo.ObterDescricao()).ToList()
			});
		}

		respostaDitado.Nota = totalLacunas > 0 ? (decimal)acertos / totalLacunas * 100 : 0;

		_context.RespostaDitados.Add(respostaDitado);
		await _context.SaveChangesAsync();

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
			.Where(d => d.Ativo && (usuarioId == null || administradorGlobal ||
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
			Categorias = ditado.DitadoCategorias
				.Select(dc => new CategoriaSimplificadaDto
				{
					Id = dc.Categoria.Id,
					Nome = dc.Categoria.Nome
				})
				.ToList()
		};
	}

	public async Task<DitadoResponse?> AtualizarCategoriasAsync(int ditadoId, List<int> categoriaIds)
	{
		var ditado = await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.FirstOrDefaultAsync(d => d.Id == ditadoId);

		if (ditado == null)
			return null;

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

	public async Task<DitadoResponse?> AtualizarDitadoAsync(int id, AtualizarDitadoRequest request)
	{
		var ditado = await _context.Ditados
			.Include(d => d.DitadoCategorias)
			.FirstOrDefaultAsync(d => d.Id == id);

		if (ditado == null)
			return null;

		// Atualizar t�tulo se fornecido
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
			// Validar categorias se houver alguma
			if (request.CategoriaIds.Any())
				await _categoriaService.ValidarCategoriasAsync(request.CategoriaIds);

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

		return await ObterDitadoComCategoriasAsync(ditado.Id);
	}

	public async Task<bool> DeletarDitadoAsync(int ditadoId, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
	{
		var ditado = await _context.Ditados.FindAsync(ditadoId);

		if (ditado == null)
			return false;

		if (tipoUsuarioLogado != TipoUsuario.Administrador && ditado.AutorId != usuarioLogadoId)
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

	private IReadOnlyList<TipoErro> IdentificarTiposErro(string resposta, string esperado)
	{
		return ClassificadorTiposErro.IdentificarTiposErro(resposta, esperado);
	}

}
