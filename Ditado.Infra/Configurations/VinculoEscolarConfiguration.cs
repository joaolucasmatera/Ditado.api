using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class VinculoEscolarConfiguration : IEntityTypeConfiguration<VinculoEscolar>
{
    public void Configure(EntityTypeBuilder<VinculoEscolar> builder)
    {
        builder.ToTable("VinculosEscolares");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Papel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(v => v.Ativo).IsRequired();
        builder.Property(v => v.DataEntrada).IsRequired();

        builder.HasOne(v => v.Escola)
            .WithMany(e => e.Vinculos)
            .HasForeignKey(v => v.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Usuario)
            .WithMany(u => u.VinculosEscolares)
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.UsuarioId).IsUnique();
        builder.HasIndex(v => new { v.EscolaId, v.Papel, v.Ativo });
    }
}
