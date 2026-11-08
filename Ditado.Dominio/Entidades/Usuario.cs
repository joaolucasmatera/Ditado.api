using Ditado.Dominio.Enums;
using System.Collections.Generic;

namespace Ditado.Dominio.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? Matricula { get; set; }
    public string? CodigoCadastro { get; set; }
    public int? NumeroChamada { get; set; }
    public bool PinConfigurado { get; set; }
    public TipoUsuario Tipo { get; set; }
    public bool AdministradorGeral { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataUltimoAcesso { get; set; }

    public ICollection<VinculoEscolar> VinculosEscolares { get; set; } = new List<VinculoEscolar>();
    public ICollection<SessaoUsuario> Sessoes { get; set; } = new List<SessaoUsuario>();

    // Relacionamentos com Turma
    public ICollection<Turma> TurmasComoAluno { get; set; } = new List<Turma>();
    public ICollection<Turma> TurmasComoProfessor { get; set; } = new List<Turma>();
}
