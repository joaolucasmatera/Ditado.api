using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class AplicacaoDitadoConfiguration : IEntityTypeConfiguration<AplicacaoDitado>
{
    public void Configure(EntityTypeBuilder<AplicacaoDitado> builder)
    {
        builder.ToTable("AplicacoesDitados");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.DataAtribuicao).IsRequired();
        builder.Property(a => a.Ativa).IsRequired();
        builder.HasIndex(a => new { a.TurmaId, a.DitadoId }).IsUnique();
        builder.HasIndex(a => new { a.EscolaId, a.Ativa });
        builder.HasOne(a => a.Escola).WithMany().HasForeignKey(a => a.EscolaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Turma).WithMany().HasForeignKey(a => a.TurmaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Ditado).WithMany().HasForeignKey(a => a.DitadoId).OnDelete(DeleteBehavior.Restrict);
    }
}
