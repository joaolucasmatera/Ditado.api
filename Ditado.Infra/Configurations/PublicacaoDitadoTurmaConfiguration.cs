using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class PublicacaoDitadoTurmaConfiguration : IEntityTypeConfiguration<PublicacaoDitadoTurma>
{
    public void Configure(EntityTypeBuilder<PublicacaoDitadoTurma> builder)
    {
        builder.ToTable("PublicacoesDitadosTurmas");
        builder.HasKey(p => new { p.PublicacaoDitadoId, p.TurmaId });
        builder.HasOne(p => p.PublicacaoDitado).WithMany(x => x.TurmasAlvo).HasForeignKey(p => p.PublicacaoDitadoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Turma).WithMany().HasForeignKey(p => p.TurmaId).OnDelete(DeleteBehavior.Restrict);
    }
}
