using Ditado.Aplicacao.DTOs.Turmas;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class TurmaService
{
	private readonly DitadoDbContext _context;

	public TurmaService(DitadoDbContext context)
	{
		_context = context;
	}

	// Compatibilidade exclusiva dos testes legados; a API nunca expõe esse caminho.
	internal Task<bool> AtribuirDitadoAsync(int turmaId, AtribuirDitadoRequest request) => AtribuirDitadoLegadoAsync(turmaId, request);

	private async Task<bool> AtribuirDitadoLegadoAsync(int turmaId, AtribuirDitadoRequest request)
	{
		var turma = await _context.Turmas.Include(t => t.Alunos).FirstOrDefaultAsync(t => t.Id == turmaId);
		if (turma == null) return false;
		var ditado = await _context.Ditados.FindAsync(request.DitadoId);
		if (ditado == null) throw new InvalidOperationException("Ditado não encontrado.");
		var jaAtribuido = await _context.TurmaDitados.AnyAsync(td => td.TurmaId == turmaId && td.DitadoId == request.DitadoId)
			|| await _context.AplicacoesDitados.AnyAsync(a => a.TurmaId == turmaId && a.DitadoId == request.DitadoId);
		if (jaAtribuido) throw new InvalidOperationException("Este ditado já está atribuído a esta turma.");
		var aplicacao = new AplicacaoDitado { EscolaId = turma.EscolaId ?? 0, TurmaId = turmaId, DitadoId = request.DitadoId, DataLimite = request.DataLimite };
		foreach (var aluno in turma.Alunos) aplicacao.Participacoes.Add(new ParticipacaoAplicacao { AlunoId = aluno.Id });
		_context.AplicacoesDitados.Add(aplicacao);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<TurmaResponse> CriarTurmaAsync(CriarTurmaRequest request, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
	{
		if (!request.EscolaId.HasValue)
			throw new InvalidOperationException("Escola é obrigatória para novas turmas.");

		var escola = await _context.Escolas.FirstOrDefaultAsync(e => e.Id == request.EscolaId.Value && e.Ativa);
		if (escola == null)
			throw new InvalidOperationException("Escola ativa não encontrada.");

		if (tipoUsuarioLogado == TipoUsuario.Professor && request.ProfessorResponsavelId != usuarioLogadoId)
			throw new InvalidOperationException("Professores só podem criar turmas para si mesmos.");
		var usuarioLogado = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioLogadoId && u.Ativo);

		var vinculoSolicitante = await _context.VinculosEscolares
			.FirstOrDefaultAsync(v => v.UsuarioId == usuarioLogadoId && v.EscolaId == escola.Id && v.Ativo && v.Escola.Ativa);
		if (vinculoSolicitante == null && !(tipoUsuarioLogado == TipoUsuario.Administrador && usuarioLogado?.AdministradorGeral == true))
			throw new InvalidOperationException("Usuário não possui vínculo ativo com a escola informada.");

		var vinculoProfessor = await _context.VinculosEscolares
			.AnyAsync(v => v.UsuarioId == request.ProfessorResponsavelId && v.EscolaId == escola.Id && v.Ativo && v.Escola.Ativa);
		if (!vinculoProfessor)
			throw new InvalidOperationException("O professor responsável não possui vínculo ativo com a escola informada.");

		// Validar professor
		var professor = await _context.Usuarios.FindAsync(request.ProfessorResponsavelId);
		if (professor == null || (professor.Tipo != TipoUsuario.Professor && professor.Tipo != TipoUsuario.Administrador))
			throw new InvalidOperationException("Professor respons�vel inv�lido. Deve ser um professor ou administrador.");

		// Validar alunos se fornecidos
		List<Usuario> alunos = new List<Usuario>();
		if (request.AlunosIds.Any())
		{
			alunos = await _context.Usuarios
				.Where(u => request.AlunosIds.Contains(u.Id) && u.Tipo == TipoUsuario.Aluno)
				.ToListAsync();

			if (alunos.Count != request.AlunosIds.Count)
				throw new InvalidOperationException("Um ou mais alunos s�o inv�lidos.");
		}

		var turma = new Turma
		{
			EscolaId = escola.Id,
			Nome = request.Nome,
			Serie = request.Serie,
			AnoLetivo = request.AnoLetivo,
			Semestre = request.Semestre,
			Descricao = request.Descricao,
			ProfessorResponsavelId = request.ProfessorResponsavelId,
			Alunos = alunos,
			Ativo = true,
			DataCriacao = DateTime.UtcNow
		};

		_context.Turmas.Add(turma);
		await _context.SaveChangesAsync();

		return await MapearParaResponseAsync(turma);
	}

	public async Task<TurmaResponse?> ObterPorIdAsync(int id)
	{
		var turma = await _context.Turmas
			.Include(t => t.ProfessorResponsavel)
			.Include(t => t.Alunos)
			.FirstOrDefaultAsync(t => t.Id == id);

		return turma == null ? null : await MapearParaResponseAsync(turma);
	}

	public async Task<List<TurmaResponse>> ListarTurmasAsync(bool? apenasAtivas = true)
	{
		var query = _context.Turmas
			.Include(t => t.ProfessorResponsavel)
			.Include(t => t.Alunos)
			.AsQueryable();

		if (apenasAtivas == true)
			query = query.Where(t => t.Ativo);

		var turmas = await query.OrderBy(t => t.Serie).ThenBy(t => t.Nome).ToListAsync();

		var responses = new List<TurmaResponse>();
		foreach (var turma in turmas)
		{
			responses.Add(await MapearParaResponseAsync(turma));
		}

		return responses;
	}

	public async Task<List<TurmaResponse>> ListarTurmasPorProfessorAsync(int professorId)
	{
		var turmas = await _context.Turmas
			.Include(t => t.ProfessorResponsavel)
			.Include(t => t.Alunos)
			.Where(t => t.ProfessorResponsavelId == professorId && t.Ativo)
			.OrderBy(t => t.Serie)
			.ThenBy(t => t.Nome)
			.ToListAsync();

		var responses = new List<TurmaResponse>();
		foreach (var turma in turmas)
		{
			responses.Add(await MapearParaResponseAsync(turma));
		}

		return responses;
	}

	/// <summary>
	/// Lista turmas de um aluno espec�fico
	/// </summary>
	public async Task<List<TurmaResponse>> ListarTurmasPorAlunoAsync(int alunoId)
	{
		var turmas = await _context.Turmas
			.Include(t => t.ProfessorResponsavel)
			.Include(t => t.Alunos)
			.Where(t => t.Alunos.Any(a => a.Id == alunoId) && t.Ativo)
			.OrderBy(t => t.Serie)
			.ThenBy(t => t.Nome)
			.ToListAsync();

		var responses = new List<TurmaResponse>();
		foreach (var turma in turmas)
		{
			responses.Add(await MapearParaResponseAsync(turma));
		}

		return responses;
	}

	public async Task<TurmaResponse?> AtualizarTurmaAsync(int id, AtualizarTurmaRequest request)
	{
		var turma = await _context.Turmas
			.Include(t => t.Alunos)
			.FirstOrDefaultAsync(t => t.Id == id);

		if (turma == null)
			return null;

		if (!string.IsNullOrWhiteSpace(request.Nome))
			turma.Nome = request.Nome;

		if (request.Serie.HasValue)
			turma.Serie = request.Serie.Value;

		if (request.AnoLetivo.HasValue)
			turma.AnoLetivo = request.AnoLetivo.Value;

		if (request.Semestre != null)
			turma.Semestre = string.IsNullOrWhiteSpace(request.Semestre) ? null : request.Semestre;

		if (request.Descricao != null)
			turma.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao;

		if (request.ProfessorResponsavelId.HasValue)
		{
			var professor = await _context.Usuarios.FindAsync(request.ProfessorResponsavelId.Value);
			if (professor == null || (professor.Tipo != TipoUsuario.Professor && professor.Tipo != TipoUsuario.Administrador))
				throw new InvalidOperationException("Professor respons�vel inv�lido.");

			turma.ProfessorResponsavelId = request.ProfessorResponsavelId.Value;
		}

		if (request.AlunosIds != null)
		{
			var novosAlunos = await _context.Usuarios
				.Where(u => request.AlunosIds.Contains(u.Id) && u.Tipo == TipoUsuario.Aluno)
				.ToListAsync();

			if (novosAlunos.Count != request.AlunosIds.Count)
				throw new InvalidOperationException("Um ou mais alunos s�o inv�lidos.");

			turma.Alunos.Clear();
			foreach (var aluno in novosAlunos)
			{
				turma.Alunos.Add(aluno);
			}
		}

		if (request.Ativo.HasValue)
			turma.Ativo = request.Ativo.Value;

		await _context.SaveChangesAsync();

		return await MapearParaResponseAsync(turma);
	}

	public async Task<bool> ExcluirTurmaAsync(int id, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
	{
		var turma = await _context.Turmas.FindAsync(id);
		if (turma == null)
			return false;

		// Professor s� pode excluir suas pr�prias turmas
		if (tipoUsuarioLogado == TipoUsuario.Professor)
		{
			if (turma.ProfessorResponsavelId != usuarioLogadoId)
				throw new InvalidOperationException("Voc� s� pode excluir turmas onde � respons�vel.");
		}

		// Admin pode excluir qualquer turma (sem valida��o adicional)

		_context.Turmas.Remove(turma);
		await _context.SaveChangesAsync();

		return true;
	}

	/// <summary>
	/// Adiciona um aluno a uma turma
	/// </summary>
	public async Task<TurmaResponse?> AdicionarAlunoAsync(int turmaId, int alunoId)
	{
		var turma = await _context.Turmas
			.Include(t => t.Alunos)
			.FirstOrDefaultAsync(t => t.Id == turmaId);

		if (turma == null)
			return null;

		// Verificar se aluno existe e � realmente um aluno
		var aluno = await _context.Usuarios.FindAsync(alunoId);
		if (aluno == null || aluno.Tipo != TipoUsuario.Aluno)
			throw new InvalidOperationException("Aluno n�o encontrado ou ID n�o corresponde a um aluno.");

		// Verificar se j� est� na turma
		if (turma.Alunos.Any(a => a.Id == alunoId))
			throw new InvalidOperationException("Aluno j� est� matriculado nesta turma.");

		turma.Alunos.Add(aluno);
		await _context.SaveChangesAsync();

		return await MapearParaResponseAsync(turma);
	}

	/// <summary>
	/// Remove um aluno de uma turma
	/// </summary>
	public async Task<TurmaResponse?> RemoverAlunoAsync(int turmaId, int alunoId)
	{
		var turma = await _context.Turmas
			.Include(t => t.Alunos)
			.FirstOrDefaultAsync(t => t.Id == turmaId);

		if (turma == null)
			return null;

		// Verificar se aluno est� na turma
		var aluno = turma.Alunos.FirstOrDefault(a => a.Id == alunoId);
		if (aluno == null)
			throw new InvalidOperationException("Aluno n�o est� matriculado nesta turma.");

		turma.Alunos.Remove(aluno);
		await _context.SaveChangesAsync();

		return await MapearParaResponseAsync(turma);
	}

	public async Task<bool> AtribuirDitadoAsync(int turmaId, AtribuirDitadoRequest request, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
	{
		var turma = await _context.Turmas
			.Include(t => t.Alunos)
			.FirstOrDefaultAsync(t => t.Id == turmaId);
		if (turma == null)
			return false;
		{
			var ator = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioLogadoId && u.Ativo);
			if (!turma.EscolaId.HasValue || (!await _context.VinculosEscolares.AnyAsync(v => v.UsuarioId == usuarioLogadoId && v.EscolaId == turma.EscolaId.Value && v.Ativo && v.Escola.Ativa) && !(tipoUsuarioLogado == TipoUsuario.Administrador && ator?.AdministradorGeral == true)))
				throw new InvalidOperationException("O usuário não possui vínculo ativo na escola da turma.");
			if (tipoUsuarioLogado != TipoUsuario.Administrador && tipoUsuarioLogado != TipoUsuario.Professor)
				throw new InvalidOperationException("Perfil sem permissão para atribuir ditados.");
		}

		var ditado = await _context.Ditados.FindAsync(request.DitadoId);
		if (ditado == null)
			throw new InvalidOperationException("Ditado n�o encontrado.");
		if (!ditado.Ativo || ditado.ArquivadoEm.HasValue)
			throw new InvalidOperationException("Ditado arquivado ou inativo não pode receber nova atribuição.");
		var autorizadoPorPublicacao = await _context.PublicacoesDitados.AnyAsync(p =>
			p.DitadoId == ditado.Id && p.ChaveVigente != null &&
			(p.Nivel == NivelVisibilidade.PublicoGeral ||
			 (p.Nivel == NivelVisibilidade.Instituicao && p.EscolaId == turma.EscolaId) ||
			 (p.Nivel == NivelVisibilidade.Turmas && p.TurmasAlvo.Any(t => t.TurmaId == turmaId))));
		if (!autorizadoPorPublicacao)
			throw new InvalidOperationException("Ditado sem publicação vigente compatível com a escola ou turma.");

		// Verifica se j� existe
		var jaAtribuido = await _context.TurmaDitados
			.AnyAsync(td => td.TurmaId == turmaId && td.DitadoId == request.DitadoId)
			|| await _context.AplicacoesDitados.AnyAsync(a => a.TurmaId == turmaId && a.DitadoId == request.DitadoId);

		if (jaAtribuido)
			throw new InvalidOperationException("Este ditado j� est� atribu�do a esta turma.");

		var escolaVinculoId = await _context.VinculosEscolares
			.Where(v => v.UsuarioId == turma.ProfessorResponsavelId && v.Ativo && v.Escola.Ativa)
			.Select(v => (int?)v.EscolaId)
			.FirstOrDefaultAsync();
		if (!turma.EscolaId.HasValue)
			throw new InvalidOperationException("Turma legada sem EscolaId não pode receber nova atribuição.");
		if (!escolaVinculoId.HasValue || escolaVinculoId != turma.EscolaId)
			throw new InvalidOperationException("O professor responsável não possui vínculo ativo na escola da turma.");
		var escolaId = turma.EscolaId.Value;

		var aplicacao = new AplicacaoDitado
		{
			EscolaId = escolaId,
			TurmaId = turmaId,
			DitadoId = request.DitadoId,
			DataAtribuicao = DateTime.UtcNow,
			DataLimite = request.DataLimite,
			Ativa = true
		};

		_context.AplicacoesDitados.Add(aplicacao);
		foreach (var aluno in turma.Alunos)
			aplicacao.Participacoes.Add(new ParticipacaoAplicacao { AlunoId = aluno.Id });
		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<bool> AtualizarAtribuicaoAsync(int turmaId, int ditadoId, AtualizarAtribuicaoRequest request)
	{
		var aplicacao = await _context.AplicacoesDitados
			.FirstOrDefaultAsync(a => a.TurmaId == turmaId && a.DitadoId == ditadoId);

		if (aplicacao == null)
			return false;

		aplicacao.DataLimite = request.DataLimite;
		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<bool> RemoverAtribuicaoAsync(int turmaId, int ditadoId)
	{
		var aplicacao = await _context.AplicacoesDitados
			.FirstOrDefaultAsync(a => a.TurmaId == turmaId && a.DitadoId == ditadoId);

		if (aplicacao == null)
			return false;

		aplicacao.Ativa = false;
		await _context.SaveChangesAsync();

		return true;
	}

	private async Task<TurmaResponse> MapearParaResponseAsync(Turma turma)
	{
		// Garantir que relacionamentos est�o carregados
		if (turma.ProfessorResponsavel == null)
		{
			await _context.Entry(turma).Reference(t => t.ProfessorResponsavel).LoadAsync();
		}
		if (!turma.Alunos.Any())
		{
			await _context.Entry(turma).Collection(t => t.Alunos).LoadAsync();
		}

		return new TurmaResponse
		{
			Id = turma.Id,
			Nome = turma.Nome,
			Serie = turma.Serie,
			AnoLetivo = turma.AnoLetivo,
			Semestre = turma.Semestre,
			Descricao = turma.Descricao,
			Ativo = turma.Ativo,
			DataCriacao = turma.DataCriacao,
			ProfessorResponsavelId = turma.ProfessorResponsavelId,
			ProfessorResponsavelNome = turma.ProfessorResponsavel?.Nome ?? "N/A",
			TotalAlunos = turma.Alunos.Count,
			Alunos = turma.Alunos.Select(a => new UsuarioResponse
			{
				Id = a.Id,
				Nome = a.Nome,
				Login = a.Login,
				Matricula = a.Matricula,
				Tipo = a.Tipo.ToString(),
				Ativo = a.Ativo,
				DataCriacao = a.DataCriacao,
				DataUltimoAcesso = a.DataUltimoAcesso
			}).ToList()
		};
	}
}


