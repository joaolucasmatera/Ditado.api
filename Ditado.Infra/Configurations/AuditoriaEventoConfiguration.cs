using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class AuditoriaEventoConfiguration : IEntityTypeConfiguration<AuditoriaEvento>
{
    public void Configure(EntityTypeBuilder<AuditoriaEvento> builder)
    {
        builder.ToTable("AuditoriaEventos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Operacao).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Recurso).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Resultado).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Motivo).HasMaxLength(500);
        builder.Property(a => a.CorrelacaoId).HasMaxLength(100);
        builder.Property(a => a.DataUtc).IsRequired();
        builder.HasIndex(a => new { a.EscolaId, a.DataUtc, a.Id });
        builder.HasIndex(a => new { a.UsuarioId, a.DataUtc });
    }
}
