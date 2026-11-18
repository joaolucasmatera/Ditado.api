using Ditado.Aplicacao.DTOs.Alunos;
using Ditado.Aplicacao.DTOs;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Extensions;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class AlunoService
{
	private readonly DitadoDbContext _context;

	public AlunoService(DitadoDbContext context)
	{
		_context = context;
	}

	public async Task<List<DitadoAlunoResponse>> ListarMeusDitadosAsync(int alunoId)
	{
		// 1. Buscar todas as turmas do aluno
		var turmasDoAluno = await _context.Usuarios
			.Where(u => u.Id == alunoId)
			.SelectMany(u => u.TurmasComoAluno)
			.Select(t => t.Id)
			.ToListAsync();

		if (!turmasDoAluno.Any())
			return new List<DitadoAlunoResponse>();

		// 2. Buscar ditados atribu�dos �s turmas do aluno
		var ditadosAtribuidos = await _context.TurmaDitados
			.Where(td => turmasDoAluno.Contains(td.TurmaId))
			.Include(td => td.Ditado)
			.ThenInclude(d => d.DitadoCategorias)
			.ThenInclude(dc => dc.Categoria)
			.Include(td => td.Turma)
			.GroupBy(td => td.DitadoId)
			.Select(g => new
			{
				DitadoId = g.Key,
				Ditado = g.First().Ditado,
				Turmas = g.Select(td => new TurmaAtribuicaoDto
				{
					TurmaId = td.TurmaId,
					TurmaNome = td.Turma.Nome,
					DataAtribuicao = td.DataAtribuicao
				}).ToList(),
				DataLimite = g.Min(td => td.DataLimite) // Pega a data limite mais pr�xima
			})
			.ToListAsync();

		// 3. Buscar tentativas do aluno
		var ditadoIds = ditadosAtribuidos.Select(d => d.DitadoId).ToList();
		var tentativas = await _context.RespostaDitados
			.Where(r => r.AlunoId == alunoId && ditadoIds.Contains(r.DitadoId))
			.GroupBy(r => r.DitadoId)
			.Select(g => new
			{
				DitadoId = g.Key,
				Tentativas = g.Count(),
				UltimaTentativaEm = g.Max(r => r.DataRealizacao),
				MelhorNota = g.Max(r => r.Nota)
			})
			.ToListAsync();

		// 4. Montar response
		var resultado = ditadosAtribuidos.Select(d =>
		{
			var tentativaInfo = tentativas.FirstOrDefault(t => t.DitadoId == d.DitadoId);
			var agora = DateTime.UtcNow;

			return new DitadoAlunoResponse
			{
				DitadoId = d.DitadoId,
				Titulo = d.Ditado.Titulo,
				Descricao = d.Ditado.Descricao,
				DataLimite = d.DataLimite,
				Atrasado = agora > d.DataLimite,
				Turmas = d.Turmas,
				Status = new StatusDitadoDto
				{
					JaTentou = tentativaInfo != null,
					Tentativas = tentativaInfo?.Tentativas ?? 0,
					UltimaTentativaEm = tentativaInfo?.UltimaTentativaEm,
					MelhorNota = tentativaInfo?.MelhorNota
				},
				CategoriasNomes = d.Ditado.DitadoCategorias
					.Select(dc => dc.Categoria.Nome)
					.ToList()
			};
		})
		.OrderBy(d => d.DataLimite) // Mais pr�ximo de vencer primeiro
		.ToList();

		// Adaptador de transição: novas aplicações são a fonte de escrita, enquanto
		// TurmaDitado permanece disponível para leitura do histórico legado.
		var aplicacoes = await _context.AplicacoesDitados
			.Where(a => a.Ativa && a.Participacoes.Any(p => p.AlunoId == alunoId && !p.RevogadaEm.HasValue))
			.Include(a => a.Ditado)
			.ThenInclude(d => d.DitadoCategorias)
			.ThenInclude(dc => dc.Categoria)
			.Include(a => a.Turma)
			.ToListAsync();
		foreach (var aplicacao in aplicacoes)
		{
			var tentativasAplicacao = await _context.RespostaDitados
				.Where(r => r.AlunoId == alunoId && r.AplicacaoDitadoId == aplicacao.Id)
			.ToListAsync();
			var existente = resultado.FirstOrDefault(r => r.DitadoId == aplicacao.DitadoId);
			if (existente == null)
			{
				resultado.Add(new DitadoAlunoResponse
				{
					DitadoId = aplicacao.DitadoId,
					Titulo = aplicacao.Ditado.Titulo,
					Descricao = aplicacao.Ditado.Descricao,
					DataLimite = aplicacao.DataLimite ?? DateTime.MaxValue,
					Atrasado = aplicacao.DataLimite.HasValue && DateTime.UtcNow > aplicacao.DataLimite.Value,
					Turmas = new List<TurmaAtribuicaoDto> { new() { TurmaId = aplicacao.TurmaId, TurmaNome = aplicacao.Turma.Nome, DataAtribuicao = aplicacao.DataAtribuicao } },
					Status = new StatusDitadoDto
					{
						JaTentou = tentativasAplicacao.Count > 0,
						Tentativas = tentativasAplicacao.Count,
						UltimaTentativaEm = tentativasAplicacao.Count == 0 ? null : tentativasAplicacao.Max(t => t.DataRealizacao),
						MelhorNota = tentativasAplicacao.Count == 0 ? null : tentativasAplicacao.Max(t => t.Nota)
					},
					CategoriasNomes = aplicacao.Ditado.DitadoCategorias.Select(dc => dc.Categoria.Nome).ToList()
				});
			}
		}

		return resultado.OrderBy(d => d.DataLimite).ToList();
	}

	public async Task<ResultadoDitadoResponse?> ObterMeuResultadoAsync(int alunoId, int tentativaId)
	{
		var tentativa = await _context.RespostaDitados.AsNoTracking()
			.Where(r => r.Id == tentativaId && r.AlunoId == alunoId &&
				_context.Usuarios.Any(u => u.Id == alunoId && u.Ativo) &&
				(r.EscolaId == null || _context.VinculosEscolares.Any(v =>
					v.UsuarioId == alunoId && v.EscolaId == r.EscolaId && v.Ativo && v.Escola.Ativa)))
			.Include(r => r.RespostasSegmentos).ThenInclude(s => s.Segmento)
			.Include(r => r.RespostasSegmentos).ThenInclude(s => s.TiposErro).ThenInclude(t => t.ErroCatalogo)
			.Include(r => r.RespostasSegmentos).ThenInclude(s => s.AnaliseLlm).ThenInclude(a => a!.Candidatos)
			.AsSplitQuery()
			.SingleOrDefaultAsync();
		if (tentativa is null) return null;

		var detalhes = tentativa.RespostasSegmentos.OrderBy(s => s.Segmento.Ordem).Select(s => new DetalheRespostaDto
		{
			SegmentoId = s.SegmentoId,
			RespostaFornecida = s.RespostaFornecida,
			RespostaEsperada = s.RespostaEsperada ?? s.Segmento.Conteudo,
			RespostaAusente = s.RespostaAusente,
			Correto = s.Correto,
			EstadoClassificacao = s.EstadoClassificacao.ToString(),
			MetodoClassificacao = s.MetodoClassificacao?.ToString(),
			EstadoAnaliseLlm = s.AnaliseLlm?.Estado.ToString(),
			TiposErro = s.TiposErro.Select(t => t.TipoErro.ObterDescricao())
				.Concat(s.TipoErro is { } legado ? [legado.ObterDescricao()] : Array.Empty<string>()).Distinct().ToList(),
			Classificacoes = s.TiposErro.Where(t => t.ErroCatalogo != null)
				.Select(t => new ClassificacaoCatalogoDto(t.ErroCatalogoId!.Value, t.ErroCatalogo!.Codigo, t.ErroCatalogo.Nome, (int)t.TipoErro))
				.Concat(s.AnaliseLlm?.Candidatos.Where(c => c.Estado == LlmCandidatoEstado.Correspondente)
					.Select(c => new ClassificacaoCatalogoDto(c.ErroCatalogoId, c.Codigo, c.Nome, null, "LLM")) ?? [])
				.ToList()
		}).ToList();
		return new ResultadoDitadoResponse
		{
			RespostaDitadoId = tentativa.Id, Nota = tentativa.Nota, TotalLacunas = detalhes.Count,
			Acertos = detalhes.Count(d => d.Correto), Erros = detalhes.Count(d => !d.Correto), Detalhes = detalhes
		};
	}

	public async Task<List<TentativaDitadoResponse>> ListarMinhasTentativasAsync(int alunoId, int ditadoId)
	{
		// Buscar data limite do ditado para o aluno (considera a primeira turma que atribuiu)
		var dataLimite = await _context.TurmaDitados
			.Where(td => td.DitadoId == ditadoId)
			.Join(_context.Usuarios.Where(u => u.Id == alunoId).SelectMany(u => u.TurmasComoAluno),
				td => td.TurmaId,
				t => t.Id,
				(td, t) => td.DataLimite)
			.FirstOrDefaultAsync();
		if (dataLimite == default)
			dataLimite = await _context.AplicacoesDitados
				.Where(a => a.DitadoId == ditadoId && a.Ativa && a.Participacoes.Any(p => p.AlunoId == alunoId && !p.RevogadaEm.HasValue))
				.Select(a => a.DataLimite ?? DateTime.MaxValue)
				.FirstOrDefaultAsync();

		var tentativas = await _context.RespostaDitados
			.Where(r => r.AlunoId == alunoId && r.DitadoId == ditadoId)
			.Include(r => r.RespostasSegmentos)
			.OrderBy(r => r.DataRealizacao) // Primeira tentativa primeiro
			.Select(r => new TentativaDitadoResponse
			{
				TentativaId = r.Id,
				DataRealizacao = r.DataRealizacao,
				Nota = r.Nota,
				TotalLacunas = r.RespostasSegmentos.Count,
				Acertos = r.RespostasSegmentos.Count(rs => rs.Correto),
				Erros = r.RespostasSegmentos.Count(rs => !rs.Correto),
				Atrasado = r.DataRealizacao > dataLimite
			})
			.ToListAsync();

		return tentativas;
	}
}
