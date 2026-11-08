using Ditado.Aplicacao.DTOs.Professores;
using Ditado.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ditado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Professor")]
[Produces("application/json")]
public class ProfessoresController : ControllerBase
{
    private readonly ProfessorService _professorService;

    public ProfessoresController(ProfessorService professorService)
    {
        _professorService = professorService;
    }

    /// <summary>
    /// Lista todos os ditados atribu�dos pelo professor logado
    /// </summary>
    /// <remarks>
    /// Retorna uma lista de todos os ditados que o professor logado atribuiu a suas turmas.
    /// 
    /// **Ordena��o:** Data limite ASC (vencidos primeiro)
    /// 
    /// **Inclui:**
    /// - Dados da turma (nome, s�rie, ano letivo)
    /// - Dados do ditado (t�tulo, descri��o, categorias)
    /// - Data de atribui��o e data limite
    /// - **% de conclus�o** (quantos alunos j� fizeram)
    /// - **Nota média** (baseada no melhor resultado de cada aluno)
    /// 
    /// **Critério:** considera o melhor resultado de cada aluno; todas as tentativas permanecem preservadas.
    /// 
    /// **Exemplo de resposta:**
    /// 
    ///     [
    ///       {
    ///         "turmaId": 5,
    ///         "turmaNome": "5� Ano A",
    ///         "ditadoId": 10,
    ///         "ditadoTitulo": "Ortografia B�sica",
    ///         "dataLimite": "2024-12-10T23:59:59Z",
    ///         "vencido": true,
    ///         "totalAlunos": 25,
    ///         "alunosQueFizeram": 18,
    ///         "percentualConclusao": 72.0,
    ///         "notaMedia": 78.5
    ///       }
    ///     ]
    /// </remarks>
    /// <returns>Lista de ditados atribu�dos</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (apenas Professor e Admin)</response>
    [HttpGet("meus-ditados-atribuidos")]
    [ProducesResponseType(typeof(List<DitadoAtribuidoResumoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DitadoAtribuidoResumoDto>>> ListarMeusDitadosAtribuidos()
    {
        var professorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var resultado = await _professorService.ListarMeusDitadosAtribuidosAsync(professorId);
        return Ok(resultado);
    }

    /// <summary>
    /// Obt�m detalhes de um ditado atribu�do a uma turma (drill-down)
    /// </summary>
    /// <remarks>
    /// Retorna informa��es detalhadas sobre os resultados dos alunos em um ditado espec�fico.
    /// 
    /// **Inclui:**
    /// 
    /// **1. Lista de Alunos:**
    /// - Nome, matr�cula
    /// - Se fez o ditado
    /// - Data de entrega do melhor resultado
    /// - Nota do melhor resultado
    /// - Erro mais comum
    /// - Se entregou atrasado
    /// 
    /// **2. Gr�fico de Erros por Tipo:**
    /// - Agrega��o de todos os erros da turma
    /// - Considera o melhor resultado de cada aluno
    /// - Formato: `[{ tipoErroId, descricao, descricaoCurta, quantidade }]`
    /// 
    /// **3. Gr�fico de Erros por Palavra:**
    /// - Lista de palavras (lacunas) do ditado
    /// - Quantidade de alunos que erraram cada palavra
    /// - Percentual de erro em rela��o aos alunos que fizeram
    /// - Considera o melhor resultado de cada aluno
    /// - Ordenado pela ordem das palavras no ditado
    /// 
    /// **Critério:** considera o melhor resultado de cada aluno; todas as tentativas permanecem preservadas.
    /// 
    /// **Exemplo de resposta:**
    /// 
    ///     {
    ///       "turmaId": 5,
    ///       "turmaNome": "5� Ano A",
    ///       "ditadoId": 10,
    ///       "ditadoTitulo": "Ortografia B�sica",
    ///       "totalAlunos": 25,
    ///       "alunosQueFizeram": 18,
    ///       "percentualConclusao": 72.0,
    ///       "notaMedia": 78.5,
    ///       "alunos": [
    ///         {
    ///           "alunoId": 100,
    ///           "nome": "Jo�o Silva",
    ///           "matricula": "2024001",
    ///           "fez": true,
    ///           "dataEntrega": "2024-12-08T14:30:00Z",
    ///           "nota": 85.5,
    ///           "erroMaisComum": "Erro de acentua��o",
    ///           "atrasado": false
    ///         }
    ///       ],
    ///       "errosPorTipo": [
    ///         {
    ///           "tipoErroId": 2,
    ///           "descricao": "Erro de acentua��o",
    ///           "descricaoCurta": "Acentua��o",
    ///           "quantidade": 12
    ///         },
    ///         {
    ///           "tipoErroId": 1,
    ///           "descricao": "Erro ortogr�fico",
    ///           "descricaoCurta": "Ortografia",
    ///           "quantidade": 8
    ///         }
    ///       ],
    ///       "errosPorPalavra": [
    ///         {
    ///           "palavra": "cachorro",
    ///           "quantidadeErros": 12,
    ///           "percentualErro": 66.67
    ///         },
    ///         {
    ///           "palavra": "�rvore",
    ///           "quantidadeErros": 15,
    ///           "percentualErro": 83.33
    ///         },
    ///         {
    ///           "palavra": "tr�s",
    ///           "quantidadeErros": 8,
    ///           "percentualErro": 44.44
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    /// <param name="turmaId">ID da turma</param>
    /// <param name="ditadoId">ID do ditado</param>
    /// <returns>Detalhes da atribui��o com resultados dos alunos</returns>
    /// <response code="200">Detalhes retornados com sucesso</response>
    /// <response code="401">N�o autenticado</response>
    /// <response code="403">Sem permiss�o (turma n�o pertence ao professor)</response>
    /// <response code="404">Atribui��o n�o encontrada</response>
    [HttpGet("turmas/{turmaId}/ditados/{ditadoId}/resultados")]
    [ProducesResponseType(typeof(DitadoAtribuidoDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DitadoAtribuidoDetalheDto>> ObterDetalhesAtribuicao(int turmaId, int ditadoId)
    {
        var professorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var resultado = await _professorService.ObterDetalhesAtribuicaoAsync(turmaId, ditadoId, professorId);

        if (resultado == null)
            return NotFound(new { mensagem = "Atribui��o n�o encontrada ou voc� n�o tem permiss�o para visualiz�-la." });

        return Ok(resultado);
    }
}

