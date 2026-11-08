using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class PublicacaoDitadoConfiguration : IEntityTypeConfiguration<PublicacaoDitado>
{
    public void Configure(EntityTypeBuilder<PublicacaoDitado> builder)
    {
        builder.ToTable("PublicacoesDitados");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nivel).HasConversion<int>().IsRequired();
        builder.Property(p => p.Versao).IsRequired();
        builder.Property(p => p.VigenteDesde).IsRequired();
        builder.HasIndex(p => new { p.DitadoId, p.ChaveVigente }).IsUnique();
        builder.HasIndex(p => new { p.EscolaId, p.Nivel, p.EncerradaEm });
        builder.HasOne(p => p.Ditado).WithMany(d => d.Publicacoes).HasForeignKey(p => p.DitadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Escola).WithMany().HasForeignKey(p => p.EscolaId).OnDelete(DeleteBehavior.Restrict);
    }
}
