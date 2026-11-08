using Ditado.Aplicacao.DTOs.Alunos;
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
