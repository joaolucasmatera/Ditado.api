using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public class UsuarioService
{
    private readonly DitadoDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly TokenService _tokenService;

    public UsuarioService(DitadoDbContext context, PasswordHasher passwordHasher, TokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<UsuarioResponse> CriarUsuarioAsync(CriarUsuarioRequest request)
    {
        if (!new EmailAddressAttribute().IsValid(request.Login))
            throw new InvalidOperationException("Login deve ser um email v�lido.");

        if (await _context.Usuarios.AnyAsync(u => u.Login == request.Login.ToLowerInvariant()))
            throw new InvalidOperationException("Login j� est� em uso.");

        if (!request.Tipo.HasValue)
            throw new InvalidOperationException("Tipo de usu�rio � obrigat�rio.");

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Login = request.Login.ToLowerInvariant(),
            SenhaHash = _passwordHasher.Hash(request.Senha),
            Matricula = request.Matricula,
            Tipo = request.Tipo.Value,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return MapearParaResponse(usuario);
    }

    public async Task<UsuarioResponse> SolicitarAcessoAsync(CriarUsuarioRequest request)
    {
        if (!new EmailAddressAttribute().IsValid(request.Login))
            throw new InvalidOperationException("Login deve ser um email v�lido.");

        var loginNormalizado = request.Login.ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.Login == loginNormalizado))
            throw new InvalidOperationException("Este email j� est� cadastrado no sistema.");

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Login = loginNormalizado,
            SenhaHash = _passwordHasher.Hash(request.Senha),
            Matricula = request.Matricula,
            Tipo = TipoUsuario.AcessoSolicitado,
            Ativo = false,
            DataCriacao = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return MapearParaResponse(usuario);
    }

    public async Task<UsuarioResponse> CadastrarProfessorAsync(CadastroProfessorRequest request)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Login == login)) throw new InvalidOperationException("Este email já está cadastrado no sistema.");
        Escola escola;
        var ativo = false;
        if (request.OpcaoEscola == "CriarNova")
        {
            if (string.IsNullOrWhiteSpace(request.NomeNovaEscola)) throw new InvalidOperationException("Informe o nome da nova escola.");
            escola = new Escola { Nome = request.NomeNovaEscola.Trim(), Codigo = $"ESC-{Guid.NewGuid():N}"[..12], Ativa = true };
            ativo = true;
            _context.Escolas.Add(escola);
        }
        else if (request.OpcaoEscola == "SelecionarExistente" && request.EscolaId.HasValue)
        {
            escola = await _context.Escolas.SingleOrDefaultAsync(e => e.Id == request.EscolaId.Value && e.Ativa) ?? throw new InvalidOperationException("Escola não encontrada.");
        }
        else throw new InvalidOperationException("Selecione criar uma escola ou escolher uma escola existente.");
        var professor = new Usuario { Nome = request.Nome.Trim(), Login = login, SenhaHash = _passwordHasher.Hash(request.Senha), Tipo = TipoUsuario.Professor, Ativo = ativo, DataCriacao = DateTime.UtcNow };
        _context.Usuarios.Add(professor);
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = professor.Id, EscolaId = escola.Id, Papel = request.OpcaoEscola == "CriarNova" ? PapelEscolar.Administrador : PapelEscolar.Professor, Ativo = ativo });
        await _context.SaveChangesAsync();
        return MapearParaResponse(professor);
    }

    public async Task<UsuarioResponse> SolicitarAcessoAlunoAsync(SolicitarAcessoAlunoRequest request)
    {
        var matricula = request.Matricula.Trim();
        if (await _context.Usuarios.AnyAsync(u => u.Matricula == matricula))
            throw new InvalidOperationException("Esta matrícula já está cadastrada no sistema.");
        string codigo;
        do { codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(5)); }
        while (await _context.Usuarios.AnyAsync(u => u.CodigoCadastro == codigo));
        var usuario = new Usuario { Nome = string.IsNullOrWhiteSpace(request.Nome) ? $"Aluno {matricula}" : request.Nome.Trim(), Login = $"aluno-{codigo.ToLowerInvariant()}@pendente.ditado", SenhaHash = string.Empty, Matricula = matricula, CodigoCadastro = codigo, Tipo = TipoUsuario.AcessoSolicitado, Ativo = false, PinConfigurado = false, DataCriacao = DateTime.UtcNow };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return MapearParaResponse(usuario);
    }

    public async Task<LoginResponse?> LoginAlunoAsync(LoginAlunoRequest request)
    {
        var identificador = request.MatriculaOuCodigo.Trim();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Tipo == TipoUsuario.Aluno && (u.Matricula == identificador || u.CodigoCadastro == identificador));
        if (usuario == null || !usuario.PinConfigurado || !_passwordHasher.Verify(request.Pin, usuario.SenhaHash)) return null;
        if (!usuario.Ativo) throw new InvalidOperationException("Sua conta está inativa. Entre em contato com a escola.");
        return await CriarSessaoAsync(usuario);
    }

    public async Task<UsuarioResponse?> ConfigurarPrimeiroPinAlunoAsync(PrimeiroAcessoAlunoRequest request)
    {
        var identificador = request.MatriculaOuCodigo.Trim();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
            (u.Tipo == TipoUsuario.AcessoSolicitado || u.Tipo == TipoUsuario.Aluno) &&
            !u.PinConfigurado &&
            (u.Matricula == identificador || u.CodigoCadastro == identificador));
        if (usuario is null) return null;
        usuario.SenhaHash = _passwordHasher.Hash(request.Pin);
        usuario.PinConfigurado = true;
        await _context.SaveChangesAsync();
        return MapearParaResponse(usuario);
    }

    public async Task<bool> VerificarPrimeiroAcessoAlunoAsync(VerificarPrimeiroAcessoAlunoRequest request)
    {
        var identificador = request.MatriculaOuCodigo.Trim();
        return await _context.Usuarios.AnyAsync(u =>
            (u.Tipo == TipoUsuario.AcessoSolicitado || u.Tipo == TipoUsuario.Aluno) &&
            !u.PinConfigurado &&
            (u.Matricula == identificador || u.CodigoCadastro == identificador));
    }

    public async Task<UsuarioResponse> CadastrarAlunoManualAsync(CadastrarAlunoManualRequest request, int atorId)
    {
        var ator = await _context.Usuarios.FindAsync(atorId);
        if (ator is null || !ator.Ativo || (ator.Tipo != TipoUsuario.Professor && ator.Tipo != TipoUsuario.Administrador))
            throw new InvalidOperationException("Apenas professor ou administrador pode cadastrar alunos.");
        var turma = await _context.Turmas.Include(t => t.Alunos).Include(t => t.Escola).SingleOrDefaultAsync(t => t.Id == request.TurmaId && t.Ativo);
        if (turma is null) throw new InvalidOperationException("Turma não encontrada ou inativa.");
        if (ator.Tipo == TipoUsuario.Professor && turma.ProfessorResponsavelId != atorId)
            throw new InvalidOperationException("Professor não autorizado para esta turma.");
        if (await _context.Usuarios.AnyAsync(u => u.Tipo == TipoUsuario.Aluno && u.NumeroChamada == request.NumeroChamada && u.TurmasComoAluno.Any(t => t.Id == request.TurmaId)))
            throw new InvalidOperationException("Já existe aluno com este número de chamada na turma.");
        var escolaNome = turma.Escola?.Nome ?? $"Escola{turma.EscolaId ?? 0}";
        var codigo = $"{NormalizarCodigo(escolaNome)}{NormalizarCodigo(turma.Nome)}{request.NumeroChamada:D3}";
        if (codigo.Length > 32) codigo = codigo[..32];
        var aluno = new Usuario { Nome = request.Nome.Trim(), Login = $"aluno-{Guid.NewGuid():N}@pendente.ditado", Matricula = string.IsNullOrWhiteSpace(request.Matricula) ? null : request.Matricula.Trim(), NumeroChamada = request.NumeroChamada, CodigoCadastro = codigo, SenhaHash = string.Empty, PinConfigurado = false, Tipo = TipoUsuario.Aluno, Ativo = true, DataCriacao = DateTime.UtcNow };
        turma.Alunos.Add(aluno);
        _context.Usuarios.Add(aluno);
        await _context.SaveChangesAsync();
        return MapearParaResponse(aluno);
    }

    public async Task<List<UsuarioResponse>> ListarSolicitacoesPendentesAsync(int atorId, TipoUsuario tipoAtor)
    {
        var escolasDoAtor = await ObterEscolasDoAtorAsync(atorId, tipoAtor);
        var administradorGeral = await EhAdministradorGeralAsync(atorId, tipoAtor);
        return await _context.Usuarios
            .Where(u => u.Tipo == TipoUsuario.AcessoSolicitado && !u.Ativo &&
                (administradorGeral || u.VinculosEscolares.Any(v => escolasDoAtor.Contains(v.EscolaId) && v.Escola.Ativa)))
            .OrderBy(u => u.DataCriacao)
            .Select(u => MapearParaResponse(u))
            .ToListAsync();
    }

    public async Task<UsuarioResponse?> AprovarAcessoAsync(int usuarioId, AprovarAcessoRequest request, int atorId, TipoUsuario tipoAprovador)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.VinculosEscolares)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);
        
        if (usuario == null)
            return null;

        if (!await PodeOperarUsuarioAsync(usuario, atorId, tipoAprovador, incluirVinculosInativos: true))
            throw new InvalidOperationException("Usuário fora do escopo escolar do aprovador.");

        if (usuario.Tipo != TipoUsuario.AcessoSolicitado)
            throw new InvalidOperationException("Este usu�rio n�o est� pendente de aprova��o.");

        if (tipoAprovador == TipoUsuario.Professor)
        {
            if (request.NovoTipo != TipoUsuario.Aluno && request.NovoTipo != TipoUsuario.Professor)
                throw new InvalidOperationException("Professores s� podem aprovar usu�rios como Aluno ou Professor.");
        }

        if (request.NovoTipo == TipoUsuario.AcessoSolicitado)
            throw new InvalidOperationException("N�o � poss�vel aprovar usu�rio com tipo 'AcessoSolicitado'.");

        usuario.Tipo = request.NovoTipo;
        usuario.Ativo = true;

        foreach (var vinculo in usuario.VinculosEscolares)
            vinculo.Ativo = true;

        await _context.SaveChangesAsync();

        return MapearParaResponse(usuario);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var loginNormalizado = request.Login.ToLowerInvariant();
        
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Login == loginNormalizado);

        if (usuario == null || !_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            return null;

        if (usuario.Tipo == TipoUsuario.AcessoSolicitado)
            throw new InvalidOperationException("Seu acesso ainda n�o foi aprovado. Aguarde a aprova��o de um administrador ou professor.");

        if (!usuario.Ativo)
            throw new InvalidOperationException("Sua conta est� inativa. Entre em contato com o administrador.");

        return await CriarSessaoAsync(usuario);
    }

    private async Task<LoginResponse> CriarSessaoAsync(Usuario usuario)
    {
        usuario.DataUltimoAcesso = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var sessao = new SessaoUsuario { Id = Guid.NewGuid(), UsuarioId = usuario.Id, CriadaEm = DateTime.UtcNow, ExpiraEm = DateTime.UtcNow.Add(TokenService.DuracaoToken) };
        _context.SessoesUsuarios.Add(sessao);
        await _context.SaveChangesAsync();
        return new LoginResponse { Token = _tokenService.GerarToken(usuario, sessao.Id), Usuario = MapearParaResponse(usuario) };
    }


    public async Task<UsuarioResponse?> ObterPorIdAsync(int id, int? atorId = null, TipoUsuario? tipoAtor = null)
    {
        var usuario = await ConsultaUsuariosNoEscopo(atorId, tipoAtor)
            .FirstOrDefaultAsync(u => u.Id == id);

        return usuario == null ? null : MapearParaResponse(usuario);
    }

    public async Task<List<UsuarioResponse>> ListarUsuariosAsync(int? atorId = null, TipoUsuario? tipoAtor = null)
    {
        return await ConsultaUsuariosNoEscopo(atorId, tipoAtor)
            .OrderBy(u => u.Nome)
            .Select(u => MapearParaResponse(u))
            .ToListAsync();
    }

    private IQueryable<Usuario> ConsultaUsuariosNoEscopo(int? atorId, TipoUsuario? tipoAtor)
    {
        if (!atorId.HasValue || !tipoAtor.HasValue) return _context.Usuarios;
        var escolas = _context.VinculosEscolares.Where(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa).Select(v => v.EscolaId);
        var global = tipoAtor == TipoUsuario.Administrador && _context.Usuarios.Any(u => u.Id == atorId && u.Ativo && u.AdministradorGeral);
        return _context.Usuarios.Where(u => global || u.Id == atorId ||
            u.VinculosEscolares.Any(v => v.Ativo && escolas.Contains(v.EscolaId)) ||
            u.TurmasComoAluno.Any(t => t.EscolaId.HasValue && escolas.Contains(t.EscolaId.Value)));
    }

    public async Task<UsuarioResponse?> AtualizarUsuarioAsync(int id, AtualizarUsuarioRequest request, int usuarioLogadoId, TipoUsuario tipoUsuarioLogado)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return null;

        var isEdicaoPropria = id == usuarioLogadoId;

        if (!isEdicaoPropria && !await PodeOperarUsuarioAsync(usuario, usuarioLogadoId, tipoUsuarioLogado))
            throw new InvalidOperationException("Usuário fora do escopo escolar do operador.");

        // EDI��O PR�PRIA: Apenas Nome, Senha e Matr�cula
        if (isEdicaoPropria)
        {
            if (request.Tipo.HasValue)
                throw new InvalidOperationException("Voc� n�o pode alterar seu pr�prio tipo.");
            
            if (request.Ativo.HasValue)
                throw new InvalidOperationException("Voc� n�o pode alterar seu pr�prio status ativo.");
        }

        // Nome (todos podem alterar pr�prio ou Admin/Prof podem alterar de outros)
        if (!string.IsNullOrWhiteSpace(request.Nome))
            usuario.Nome = request.Nome;

        // Matr�cula (todos podem alterar pr�prio ou Admin/Prof podem alterar de outros)
        if (request.Matricula != null)
            usuario.Matricula = string.IsNullOrWhiteSpace(request.Matricula) ? null : request.Matricula;

        // Senha (todos podem alterar pr�pria)
        if (!string.IsNullOrWhiteSpace(request.SenhaAtual) && !string.IsNullOrWhiteSpace(request.SenhaNova))
        {
            if (!_passwordHasher.Verify(request.SenhaAtual, usuario.SenhaHash))
                throw new InvalidOperationException("Senha atual incorreta.");

            usuario.SenhaHash = _passwordHasher.Hash(request.SenhaNova);
        }

        // ATIVO (apenas para edi��o de outros)
        if (!isEdicaoPropria && request.Ativo.HasValue)
        {
            ValidarAlteracaoAtivo(tipoUsuarioLogado, usuario.Tipo);
            usuario.Ativo = request.Ativo.Value;
        }

        // TIPO (apenas para edi��o de outros)
        if (!isEdicaoPropria && request.Tipo.HasValue)
        {
            ValidarAlteracaoTipo(tipoUsuarioLogado, usuario.Tipo, request.Tipo.Value);
            usuario.Tipo = request.Tipo.Value;
        }

        await _context.SaveChangesAsync();

        return MapearParaResponse(usuario);
    }

    public async Task<bool> AlterarAdministradorGeralAsync(int alvoId, bool habilitar, int atorId, string motivo, string? correlacaoId = null)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Motivo é obrigatório.");
        var ator = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo);
        if (ator?.Tipo != TipoUsuario.Administrador || !ator.AdministradorGeral)
            throw new InvalidOperationException("Apenas Administrador Geral/Técnico pode alterar esta capability.");
        if (alvoId == atorId)
            throw new InvalidOperationException("Autoelevação ou auto-revogação não é permitida.");
        var alvo = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == alvoId && u.Ativo);
        if (alvo is null)
            throw new InvalidOperationException("Usuário alvo não encontrado.");
        if (alvo.Tipo != TipoUsuario.Administrador)
            throw new InvalidOperationException("O alvo deve ser um Administrador.");

        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        var antes = alvo.AdministradorGeral;
        alvo.AdministradorGeral = habilitar;
        var sessoes = await _context.SessoesUsuarios.Where(s => s.UsuarioId == alvoId && !s.RevogadaEm.HasValue).ToListAsync();
        foreach (var sessao in sessoes)
        {
            sessao.RevogadaEm = DateTime.UtcNow;
            sessao.MotivoRevogacao = habilitar ? "Capability AdministradorGeral concedida; novo login obrigatório." : "Capability AdministradorGeral revogada.";
        }
        _context.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = atorId,
            Operacao = habilitar ? "ConcederAdministradorGeral" : "RevogarAdministradorGeral",
            Recurso = $"Usuario:{alvoId}",
            Resultado = antes == habilitar ? "SemAlteracao" : "Permitido",
            Motivo = $"{motivo}; antes={antes}; depois={habilitar}",
            CorrelacaoId = correlacaoId,
            DataUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return antes != habilitar;
    }

    // VALIDA��O: Altera��o de Ativo
    private void ValidarAlteracaoAtivo(TipoUsuario tipoLogado, TipoUsuario tipoAlvo)
    {
        // Aluno n�o pode alterar ningu�m
        if (tipoLogado == TipoUsuario.Aluno)
            throw new InvalidOperationException("Alunos n�o podem alterar status de outros usu�rios.");

        // Professor s� pode alterar Alunos e AcessoSolicitado
        if (tipoLogado == TipoUsuario.Professor)
        {
            if (tipoAlvo != TipoUsuario.Aluno && tipoAlvo != TipoUsuario.AcessoSolicitado)
                throw new InvalidOperationException("Professores s� podem alterar status de Alunos e usu�rios com AcessoSolicitado.");
        }

        // Admin pode alterar qualquer um (sem valida��o adicional)
    }

    // VALIDA��O: Altera��o de Tipo
    private void ValidarAlteracaoTipo(TipoUsuario tipoLogado, TipoUsuario tipoAtual, TipoUsuario tipoNovo)
    {
        // N�o pode reduzir para AcessoSolicitado
        if (tipoNovo == TipoUsuario.AcessoSolicitado)
            throw new InvalidOperationException("N�o � poss�vel alterar usu�rio para tipo 'AcessoSolicitado'. Para bloquear acesso, desative o usu�rio.");

        var hierarquia = new Dictionary<TipoUsuario, int>
        {
            { TipoUsuario.AcessoSolicitado, 0 },
            { TipoUsuario.Aluno, 1 },
            { TipoUsuario.Professor, 2 },
            { TipoUsuario.Administrador, 3 }
        };

        var nivelAtual = hierarquia[tipoAtual];
        var nivelNovo = hierarquia[tipoNovo];
        var isAumento = nivelNovo > nivelAtual;
        var isReducao = nivelNovo < nivelAtual;

        // Aluno n�o pode alterar tipo de ningu�m
        if (tipoLogado == TipoUsuario.Aluno)
            throw new InvalidOperationException("Alunos n�o podem alterar tipo de outros usu�rios.");

        // Professor pode aumentar, mas n�o reduzir
        if (tipoLogado == TipoUsuario.Professor)
        {
            if (isReducao)
                throw new InvalidOperationException("Apenas administradores podem reduzir tipo de usu�rio.");

            // Professor s� pode aumentar Aluno > Professor (n�o pode criar Admin)
            if (isAumento && tipoNovo == TipoUsuario.Administrador)
                throw new InvalidOperationException("Professores n�o podem promover usu�rios a Administrador.");

            // Professor s� pode alterar AcessoSolicitado e Alunos
            if (tipoAtual != TipoUsuario.AcessoSolicitado && tipoAtual != TipoUsuario.Aluno)
                throw new InvalidOperationException("Professores s� podem alterar tipo de usu�rios com AcessoSolicitado ou Alunos.");
        }

        // Admin pode fazer qualquer altera��o (exceto para AcessoSolicitado, j� validado acima)
    }

    public async Task<bool> BloquearUsuarioAsync(int id, int atorId, TipoUsuario tipoAtor)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return false;

        if (!await PodeOperarUsuarioAsync(usuario, atorId, tipoAtor))
            throw new InvalidOperationException("Usuário fora do escopo escolar do operador.");

        usuario.Ativo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DesbloquearUsuarioAsync(int id, int atorId, TipoUsuario tipoAtor)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return false;

        if (!await PodeOperarUsuarioAsync(usuario, atorId, tipoAtor))
            throw new InvalidOperationException("Usuário fora do escopo escolar do operador.");

        usuario.Ativo = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RevogarSessaoAsync(Guid sessaoId, int usuarioId, string motivo)
    {
        var sessao = await _context.SessoesUsuarios
            .FirstOrDefaultAsync(s => s.Id == sessaoId && s.UsuarioId == usuarioId);

        if (sessao == null || sessao.RevogadaEm.HasValue)
        {
            _context.AuditoriaEventos.Add(new AuditoriaEvento
            {
                UsuarioId = usuarioId,
                Operacao = "RevogarSessao",
                Recurso = "SessaoUsuario",
                Resultado = "Negado",
                Motivo = "Sessão inexistente, pertencente a outro usuário ou já revogada.",
                DataUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return false;
        }

        sessao.RevogadaEm = DateTime.UtcNow;
        sessao.MotivoRevogacao = motivo;
        _context.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = usuarioId,
            Operacao = "RevogarSessao",
            Recurso = "SessaoUsuario",
            Resultado = "Permitido",
            Motivo = motivo,
            DataUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ContextoEscolarResponse>> ListarContextosEscolaresAsync(int usuarioId)
    {
        var administradorGeral = await _context.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId && u.Ativo).Select(u => u.AdministradorGeral).FirstOrDefaultAsync();
        return await _context.VinculosEscolares
            .AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId && v.Ativo && v.Escola.Ativa)
            .OrderBy(v => v.Escola.Nome)
            .Select(v => new ContextoEscolarResponse
            {
                EscolaId = v.EscolaId,
                Nome = v.Escola.Nome,
                Codigo = v.Escola.Codigo,
                Papel = v.Papel.ToString(),
                AdministradorGeral = administradorGeral
            })
            .ToListAsync();
    }

    public async Task<bool> PossuiAcessoEscolarAsync(int usuarioId, int escolaId, PapelEscolar? papel = null)
    {
        var consulta = _context.VinculosEscolares
            .AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId && v.EscolaId == escolaId && v.Ativo && v.Escola.Ativa);

        if (papel.HasValue)
            consulta = consulta.Where(v => v.Papel == papel.Value);

        return await consulta.AnyAsync();
    }

    public async Task RegistrarAuditoriaAsync(int? usuarioId, int? escolaId, string operacao, string recurso, string resultado, string motivo)
    {
        _context.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = usuarioId,
            EscolaId = escolaId,
            Operacao = operacao,
            Recurso = recurso,
            Resultado = resultado,
            Motivo = motivo,
            DataUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeletarUsuarioAsync(int id, int atorId, TipoUsuario tipoAtor)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return false;

        if (!await PodeOperarUsuarioAsync(usuario, atorId, tipoAtor))
            throw new InvalidOperationException("Usuário fora do escopo escolar do operador.");

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> PodeOperarUsuarioAsync(Usuario alvo, int atorId, TipoUsuario tipoAtor, bool incluirVinculosInativos = false)
    {
        if (alvo.Id == atorId)
            return true;
        if (await EhAdministradorGeralAsync(atorId, tipoAtor))
            return true;

        var escolasDoAtor = await ObterEscolasDoAtorAsync(atorId, tipoAtor);
        if (escolasDoAtor.Count == 0)
            return false;

        var possuiVinculo = await _context.VinculosEscolares.AnyAsync(v => v.UsuarioId == alvo.Id &&
            (incluirVinculosInativos || v.Ativo) && escolasDoAtor.Contains(v.EscolaId) && v.Escola.Ativa);
        return possuiVinculo ||
            await _context.Turmas.AnyAsync(t => t.EscolaId.HasValue && escolasDoAtor.Contains(t.EscolaId.Value) && t.Alunos.Any(a => a.Id == alvo.Id));
    }

    private async Task<List<int>> ObterEscolasDoAtorAsync(int atorId, TipoUsuario tipoAtor)
    {
        if (tipoAtor == TipoUsuario.Administrador && await _context.Usuarios.AnyAsync(u => u.Id == atorId && u.Ativo && u.AdministradorGeral))
            return await _context.Escolas.Where(e => e.Ativa).Select(e => e.Id).ToListAsync();

        return await _context.VinculosEscolares
            .Where(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa)
            .Select(v => v.EscolaId)
            .Distinct()
            .ToListAsync();
    }

    private Task<bool> EhAdministradorGeralAsync(int atorId, TipoUsuario tipoAtor)
        => _context.Usuarios.AnyAsync(u => u.Id == atorId && u.Ativo && tipoAtor == TipoUsuario.Administrador && u.AdministradorGeral);

    private static UsuarioResponse MapearParaResponse(Usuario usuario)
    {
        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Login = usuario.Login,
            Matricula = usuario.Matricula,
            CodigoCadastro = usuario.CodigoCadastro,
            Tipo = usuario.Tipo.ToString(),
            AdministradorGeral = usuario.AdministradorGeral,
            Ativo = usuario.Ativo,
            DataCriacao = usuario.DataCriacao,
            DataUltimoAcesso = usuario.DataUltimoAcesso
        };
    }

    private static string NormalizarCodigo(string valor)
        => new string(valor.Where(char.IsLetterOrDigit).ToArray());
}

public sealed class ContextoEscolarResponse
{
    public int EscolaId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Papel { get; init; } = string.Empty;
    public bool AdministradorGeral { get; init; }
}
