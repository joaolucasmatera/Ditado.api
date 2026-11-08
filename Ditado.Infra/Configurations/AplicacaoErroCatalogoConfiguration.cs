using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class AplicacaoErroCatalogoConfiguration : IEntityTypeConfiguration<AplicacaoErroCatalogo>
{
    public void Configure(EntityTypeBuilder<AplicacaoErroCatalogo> builder)
    {
        builder.ToTable("AplicacoesErrosCatalogo");
        builder.HasKey(e => new { e.AplicacaoDitadoId, e.ErroCatalogoId });
        builder.Property(e => e.CodigoSnapshot).IsRequired().HasMaxLength(40);
        builder.Property(e => e.NomeSnapshot).IsRequired().HasMaxLength(200);
        builder.HasOne(e => e.AplicacaoDitado).WithMany(a => a.ErrosSelecionados).HasForeignKey(e => e.AplicacaoDitadoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ErroCatalogo).WithMany().HasForeignKey(e => e.ErroCatalogoId).OnDelete(DeleteBehavior.Restrict);
    }
}
