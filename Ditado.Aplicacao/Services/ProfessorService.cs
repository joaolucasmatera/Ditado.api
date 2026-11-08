using Ditado.Aplicacao.DTOs.Professores;
using Ditado.Dominio.Enums;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Extensions;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class ProfessorService
{
    private readonly DitadoDbContext _context;

    public ProfessorService(DitadoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista todos os ditados atribu�dos pelo professor logado
    /// </summary>
    public async Task<List<DitadoAtribuidoResumoDto>> ListarMeusDitadosAtribuidosAsync(int professorId)
    {
        var agora = DateTime.UtcNow;

        // Buscar turmas do professor
        var turmasIds = await _context.Turmas
            .Where(t => t.ProfessorResponsavelId == professorId)
            .Select(t => t.Id)
            .ToListAsync();

        if (!turmasIds.Any())
            return new List<DitadoAtribuidoResumoDto>();

        // Buscar atribui��es de ditados
        var atribuicoes = await _context.TurmaDitados
            .Where(td => turmasIds.Contains(td.TurmaId))
            .Include(td => td.Turma)
            .Include(td => td.Ditado)
            .ThenInclude(d => d.DitadoCategorias)
            .ThenInclude(dc => dc.Categoria)
            .ToListAsync();

        var resultado = new List<DitadoAtribuidoResumoDto>();

        foreach (var atribuicao in atribuicoes)
        {
            // Total de alunos na turma
            var totalAlunos = await _context.Usuarios
                .Where(u => u.TurmasComoAluno.Any(t => t.Id == atribuicao.TurmaId))
                .CountAsync();

            // O relatório principal usa o melhor resultado de cada aluno.
            var primeiraTentativaPorAluno = await _context.RespostaDitados
                .Where(r => r.DitadoId == atribuicao.DitadoId)
                .Where(r => r.Aluno.TurmasComoAluno.Any(t => t.Id == atribuicao.TurmaId))
                .GroupBy(r => r.AlunoId)
                .Select(g => g.OrderByDescending(r => r.Nota).ThenBy(r => r.DataRealizacao).First())
                .ToListAsync();

            var alunosQueFizeram = primeiraTentativaPorAluno.Count;
            var notaMedia = primeiraTentativaPorAluno.Any() 
                ? primeiraTentativaPorAluno.Average(r => r.Nota) 
                : (decimal?)null;

            resultado.Add(new DitadoAtribuidoResumoDto
            {
                TurmaId = atribuicao.TurmaId,
                TurmaNome = atribuicao.Turma.Nome,
                Serie = atribuicao.Turma.Serie,
                AnoLetivo = atribuicao.Turma.AnoLetivo,
                DitadoId = atribuicao.DitadoId,
                DitadoTitulo = atribuicao.Ditado.Titulo,
                DitadoDescricao = atribuicao.Ditado.Descricao,
                Categorias = atribuicao.Ditado.DitadoCategorias
                    .Select(dc => dc.Categoria.Nome)
                    .ToList(),
                DataAtribuicao = atribuicao.DataAtribuicao,
                DataLimite = atribuicao.DataLimite,
                Vencido = agora > atribuicao.DataLimite,
                TotalAlunos = totalAlunos,
                AlunosQueFizeram = alunosQueFizeram,
                PercentualConclusao = totalAlunos > 0 
                    ? Math.Round((decimal)alunosQueFizeram / totalAlunos * 100, 2) 
                    : 0,
                NotaMedia = notaMedia.HasValue 
                    ? Math.Round(notaMedia.Value, 2) 
                    : null
            });
        }

        // Adaptador de transição: aplicações novas são lidas junto do legado,
        // sem duplicar gravações nem perder atribuições recém-criadas.
        var aplicacoes = await _context.AplicacoesDitados
            .Where(a => a.Turma.ProfessorResponsavelId == professorId && a.Ativa)
            .Include(a => a.Turma)
            .Include(a => a.Ditado)
            .ThenInclude(d => d.DitadoCategorias)
            .ThenInclude(dc => dc.Categoria)
            .ToListAsync();

        foreach (var aplicacao in aplicacoes.Where(a => resultado.All(r => r.TurmaId != a.TurmaId || r.DitadoId != a.DitadoId)))
        {
            var alunoIds = await _context.ParticipacoesAplicacoes
                .Where(p => p.AplicacaoDitadoId == aplicacao.Id && !p.RevogadaEm.HasValue)
                .Select(p => p.AlunoId)
                .ToListAsync();
            var primeiras = await _context.RespostaDitados
                .Where(r => r.AplicacaoDitadoId == aplicacao.Id && alunoIds.Contains(r.AlunoId))
                .GroupBy(r => r.AlunoId)
                .Select(g => g.OrderByDescending(r => r.Nota).ThenBy(r => r.DataRealizacao).First())
                .ToListAsync();
            var dataLimite = aplicacao.DataLimite ?? DateTime.MaxValue;
            resultado.Add(new DitadoAtribuidoResumoDto
            {
                TurmaId = aplicacao.TurmaId,
                TurmaNome = aplicacao.Turma.Nome,
                Serie = aplicacao.Turma.Serie,
                AnoLetivo = aplicacao.Turma.AnoLetivo,
                DitadoId = aplicacao.DitadoId,
                DitadoTitulo = aplicacao.Ditado.Titulo,
                DitadoDescricao = aplicacao.Ditado.Descricao,
                Categorias = aplicacao.Ditado.DitadoCategorias.Select(dc => dc.Categoria.Nome).ToList(),
                DataAtribuicao = aplicacao.DataAtribuicao,
                DataLimite = dataLimite,
                Vencido = agora > dataLimite,
                TotalAlunos = alunoIds.Count,
                AlunosQueFizeram = primeiras.Count,
                PercentualConclusao = alunoIds.Count > 0 ? Math.Round((decimal)primeiras.Count / alunoIds.Count * 100, 2) : 0,
                NotaMedia = primeiras.Count > 0 ? Math.Round(primeiras.Average(r => r.Nota), 2) : null
            });
        }

        return resultado.OrderBy(r => r.DataLimite).ToList();
    }

    /// <summary>
    /// Obt�m detalhes de um ditado atribu�do a uma turma
    /// </summary>
    public async Task<DitadoAtribuidoDetalheDto?> ObterDetalhesAtribuicaoAsync(int turmaId, int ditadoId, int professorId)
    {
        // Verificar se a turma pertence ao professor
        var turma = await _context.Turmas
            .Include(t => t.Alunos)
            .FirstOrDefaultAsync(t => t.Id == turmaId && t.ProfessorResponsavelId == professorId);

        if (turma == null)
            return null;

        // Buscar atribui��o com segmentos do ditado
        var atribuicao = await _context.TurmaDitados
            .Include(td => td.Ditado)
            .ThenInclude(d => d.Segmentos)
            .FirstOrDefaultAsync(td => td.TurmaId == turmaId && td.DitadoId == ditadoId);
        var possuiAtribuicaoLegada = atribuicao != null;

        var aplicacaoNova = await _context.AplicacoesDitados
            .Include(a => a.Ditado)
            .ThenInclude(d => d.Segmentos)
            .FirstOrDefaultAsync(a => a.TurmaId == turmaId && a.DitadoId == ditadoId && a.Ativa);

        if (atribuicao == null && aplicacaoNova != null)
        {
            atribuicao = new TurmaDitado
            {
                TurmaId = aplicacaoNova.TurmaId,
                DitadoId = aplicacaoNova.DitadoId,
                DataAtribuicao = aplicacaoNova.DataAtribuicao,
                DataLimite = aplicacaoNova.DataLimite ?? DateTime.MaxValue,
                Turma = turma,
                Ditado = aplicacaoNova.Ditado
            };
        }

        if (atribuicao == null)
            return null;

        // Buscar todos os alunos da turma
        var alunosDaTurma = aplicacaoNova != null && !possuiAtribuicaoLegada
            ? await _context.Usuarios.Where(u => _context.ParticipacoesAplicacoes.Any(p => p.AplicacaoDitadoId == aplicacaoNova.Id && p.AlunoId == u.Id && !p.RevogadaEm.HasValue)).ToListAsync()
            : await _context.Usuarios.Where(u => u.TurmasComoAluno.Any(t => t.Id == turmaId)).ToListAsync();

        // O relatório principal usa o melhor resultado de cada aluno.
        var consultaTentativas = _context.RespostaDitados
            .Where(r => alunosDaTurma.Select(a => a.Id).Contains(r.AlunoId));
        consultaTentativas = aplicacaoNova != null && !possuiAtribuicaoLegada
            ? consultaTentativas.Where(r => r.AplicacaoDitadoId == aplicacaoNova.Id)
            : consultaTentativas.Where(r => r.DitadoId == ditadoId);

        var primeiraTentativas = await consultaTentativas
            .Include(r => r.RespostasSegmentos)
            .ThenInclude(rs => rs.Segmento)
            .Include(r => r.RespostasSegmentos)
            .ThenInclude(rs => rs.TiposErro)
            .GroupBy(r => r.AlunoId)
            .Select(g => g.OrderByDescending(r => r.Nota).ThenBy(r => r.DataRealizacao).First())
            .ToListAsync();

        var primeiraTentativasPorAluno = primeiraTentativas.ToDictionary(r => r.AlunoId);

        // Montar lista de alunos
        var alunosResultado = new List<AlunoResultadoDto>();

        foreach (var aluno in alunosDaTurma)
        {
            var fez = primeiraTentativasPorAluno.ContainsKey(aluno.Id);
            var resposta = fez ? primeiraTentativasPorAluno[aluno.Id] : null;

            string? erroMaisComum = null;
            if (resposta != null)
            {
                var erros = resposta.RespostasSegmentos
                    .Where(rs => !rs.Correto)
                    .SelectMany(ObterTiposErro)
                    .GroupBy(tipoErro => tipoErro)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (erros != null)
                {
                    erroMaisComum = erros.Key.ObterDescricao();
                }
            }

            alunosResultado.Add(new AlunoResultadoDto
            {
                AlunoId = aluno.Id,
                Nome = aluno.Nome,
                Matricula = aluno.Matricula,
                Fez = fez,
                DataEntrega = resposta?.DataRealizacao,
                Nota = resposta != null ? Math.Round(resposta.Nota, 2) : null,
                ErroMaisComum = erroMaisComum,
                Atrasado = resposta != null && resposta.DataRealizacao > atribuicao.DataLimite
            });
        }

        // Calcular erros por tipo usando o resultado principal de cada aluno.
        var todosErros = primeiraTentativas
            .SelectMany(r => r.RespostasSegmentos)
            .Where(rs => !rs.Correto)
            .SelectMany(ObterTiposErro)
            .GroupBy(tipoErro => tipoErro)
            .Select(g => new ErrosPorTipoDto
            {
                TipoErroId = (int)g.Key,
                Descricao = g.Key.ObterDescricao(),
                DescricaoCurta = g.Key.ObterDescricaoCurta(),
                Quantidade = g.Count()
            })
            .OrderByDescending(e => e.Quantidade)
            .ToList();

        // Calcular erros por palavra usando o resultado principal de cada aluno.
        var lacunas = atribuicao.Ditado.Segmentos
            .Where(s => s.Tipo == TipoSegmento.Lacuna)
            .OrderBy(s => s.Ordem)
            .ToList();

        var errosPorPalavra = new List<ErrosPorPalavraDto>();
        var totalAlunosQueFizeram = primeiraTentativas.Count;

        foreach (var lacuna in lacunas)
        {
            // Contar quantos alunos erraram esta palavra no resultado principal.
            var quantidadeErros = primeiraTentativas
                .SelectMany(r => r.RespostasSegmentos)
                .Count(rs => rs.SegmentoId == lacuna.Id && !rs.Correto);

            errosPorPalavra.Add(new ErrosPorPalavraDto
            {
                Palavra = lacuna.Conteudo,
                QuantidadeErros = quantidadeErros,
                PercentualErro = totalAlunosQueFizeram > 0
                    ? Math.Round((decimal)quantidadeErros / totalAlunosQueFizeram * 100, 2)
                    : 0
            });
        }

        // Estat�sticas gerais
        var totalAlunos = alunosDaTurma.Count;
        var alunosQueFizeram = primeiraTentativas.Count;
        var notaMedia = primeiraTentativas.Any()
            ? Math.Round(primeiraTentativas.Average(r => r.Nota), 2)
            : (decimal?)null;

        return new DitadoAtribuidoDetalheDto
        {
            TurmaId = turma.Id,
            TurmaNome = turma.Nome,
            Serie = turma.Serie,
            AnoLetivo = turma.AnoLetivo,
            DitadoId = atribuicao.DitadoId,
            DitadoTitulo = atribuicao.Ditado.Titulo,
            DitadoDescricao = atribuicao.Ditado.Descricao,
            DataAtribuicao = atribuicao.DataAtribuicao,
            DataLimite = atribuicao.DataLimite,
            TotalAlunos = totalAlunos,
            AlunosQueFizeram = alunosQueFizeram,
            PercentualConclusao = totalAlunos > 0
                ? Math.Round((decimal)alunosQueFizeram / totalAlunos * 100, 2)
                : 0,
            NotaMedia = notaMedia,
            Alunos = alunosResultado.OrderBy(a => a.Nome).ToList(),
            ErrosPorTipo = todosErros,
            ErrosPorPalavra = errosPorPalavra
        };
    }

    private static IEnumerable<TipoErro> ObterTiposErro(RespostaSegmento resposta)
    {
        if (resposta.TiposErro.Count > 0)
            return resposta.TiposErro.Select(tipo => tipo.TipoErro);

        return resposta.TipoErro.HasValue
            ? new[] { resposta.TipoErro.Value }
            : Array.Empty<TipoErro>();
    }
}


