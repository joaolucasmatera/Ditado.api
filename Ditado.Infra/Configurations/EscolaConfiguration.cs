using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class EscolaConfiguration : IEntityTypeConfiguration<Escola>
{
    public void Configure(EntityTypeBuilder<Escola> builder)
    {
        builder.ToTable("Escolas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Codigo).IsUnique();
        builder.Property(e => e.Ativa).IsRequired();
        builder.Property(e => e.DataCriacao).IsRequired();
    }
}
