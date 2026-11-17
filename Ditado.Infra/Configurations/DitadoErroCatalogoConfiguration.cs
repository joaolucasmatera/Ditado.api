using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class DitadoErroCatalogoConfiguration : IEntityTypeConfiguration<DitadoErroCatalogo>
{
    public void Configure(EntityTypeBuilder<DitadoErroCatalogo> builder)
    {
        builder.ToTable("DitadosErrosCatalogo");
        builder.HasKey(e => new { e.DitadoId, e.ErroCatalogoId });
        builder.HasOne(e => e.Ditado).WithMany(d => d.ErrosCatalogoSelecionados)
            .HasForeignKey(e => e.DitadoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ErroCatalogo).WithMany()
            .HasForeignKey(e => e.ErroCatalogoId).OnDelete(DeleteBehavior.Restrict);
    }
}
