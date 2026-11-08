using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class SessaoUsuarioConfiguration : IEntityTypeConfiguration<SessaoUsuario>
{
    public void Configure(EntityTypeBuilder<SessaoUsuario> builder)
    {
        builder.ToTable("SessoesUsuarios");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.MotivoRevogacao).HasMaxLength(200);
        builder.Property(s => s.CriadaEm).IsRequired();
        builder.Property(s => s.ExpiraEm).IsRequired();
        builder.HasOne(s => s.Usuario).WithMany(u => u.Sessoes).HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.UsuarioId, s.RevogadaEm, s.ExpiraEm });
    }
}
