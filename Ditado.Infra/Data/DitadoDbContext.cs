using Ditado.Dominio.Entidades;
using Ditado.Infra.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Infra.Data;

public class DitadoDbContext : DbContext
{
    public DitadoDbContext(DbContextOptions<DitadoDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Dominio.Entidades.Ditado> Ditados { get; set; }
    public DbSet<DitadoSegmento> DitadoSegmentos { get; set; }
    public DbSet<RespostaDitado> RespostaDitados { get; set; }
    public DbSet<RespostaSegmento> RespostaSegmentos { get; set; }
    public DbSet<RespostaSegmentoTipoErro> RespostaSegmentoTiposErro { get; set; }
    public DbSet<Turma> Turmas { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<ErroCatalogo> ErrosCatalogo { get; set; }
    public DbSet<DitadoCategoria> DitadoCategorias { get; set; }
    public DbSet<TurmaDitado> TurmaDitados { get; set; }
    public DbSet<Escola> Escolas { get; set; }
    public DbSet<VinculoEscolar> VinculosEscolares { get; set; }
    public DbSet<SessaoUsuario> SessoesUsuarios { get; set; }
    public DbSet<AuditoriaEvento> AuditoriaEventos { get; set; }
    public DbSet<AplicacaoDitado> AplicacoesDitados { get; set; }
    public DbSet<ParticipacaoTurma> ParticipacoesTurmas { get; set; }
    public DbSet<ParticipacaoAplicacao> ParticipacoesAplicacoes { get; set; }
    public DbSet<AplicacaoErroCatalogo> AplicacoesErrosCatalogo { get; set; }
    public DbSet<PublicacaoDitado> PublicacoesDitados { get; set; }
    public DbSet<PublicacaoDitadoTurma> PublicacoesDitadosTurmas { get; set; }
    public DbSet<PublicacaoErroCatalogo> PublicacoesErrosCatalogo { get; set; }
    public DbSet<PublicacaoErroCatalogoTurma> PublicacoesErrosCatalogoTurmas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new DitadoConfiguration());
        modelBuilder.ApplyConfiguration(new DitadoSegmentoConfiguration());
        modelBuilder.ApplyConfiguration(new RespostaDitadoConfiguration());
        modelBuilder.ApplyConfiguration(new RespostaSegmentoConfiguration());
        modelBuilder.ApplyConfiguration(new RespostaSegmentoTipoErroConfiguration());
        modelBuilder.ApplyConfiguration(new TurmaConfiguration());
        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new ErroCatalogoConfiguration());
        modelBuilder.ApplyConfiguration(new DitadoCategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new TurmaDitadoConfiguration());
        modelBuilder.ApplyConfiguration(new EscolaConfiguration());
        modelBuilder.ApplyConfiguration(new VinculoEscolarConfiguration());
        modelBuilder.ApplyConfiguration(new SessaoUsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new AuditoriaEventoConfiguration());
        modelBuilder.ApplyConfiguration(new AplicacaoDitadoConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipacaoTurmaConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipacaoAplicacaoConfiguration());
        modelBuilder.ApplyConfiguration(new AplicacaoErroCatalogoConfiguration());
        modelBuilder.ApplyConfiguration(new PublicacaoDitadoConfiguration());
        modelBuilder.ApplyConfiguration(new PublicacaoDitadoTurmaConfiguration());
        modelBuilder.ApplyConfiguration(new PublicacaoErroCatalogoConfiguration());
        modelBuilder.ApplyConfiguration(new PublicacaoErroCatalogoTurmaConfiguration());
    }
}
