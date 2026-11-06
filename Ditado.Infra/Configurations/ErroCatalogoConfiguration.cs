using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class ErroCatalogoConfiguration : IEntityTypeConfiguration<ErroCatalogo>
{
    public void Configure(EntityTypeBuilder<ErroCatalogo> builder)
    {
        builder.ToTable("ErrosCatalogo");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Codigo).IsRequired().HasMaxLength(40);
        builder.HasIndex(e => e.Codigo).IsUnique();
        builder.Property(e => e.Nome).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Descricao).IsRequired().HasMaxLength(10000).HasColumnType("text");
        builder.Property(e => e.ExemploEsperado).HasMaxLength(2000);
        builder.Property(e => e.ExemploResposta).HasMaxLength(2000);
        builder.Property(e => e.ObservacaoPedagogica).HasMaxLength(10000).HasColumnType("text");
        builder.Property(e => e.SerieAno).HasMaxLength(200);
        builder.HasOne(e => e.Autor).WithMany().HasForeignKey(e => e.AutorId).OnDelete(DeleteBehavior.Restrict);
    }
}
