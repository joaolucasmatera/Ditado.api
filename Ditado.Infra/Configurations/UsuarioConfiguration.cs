using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(u => u.Login)
            .IsUnique();
        
        builder.Property(u => u.SenhaHash)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(u => u.Matricula)
            .HasMaxLength(20);

        builder.Property(u => u.CodigoCadastro).HasMaxLength(32);
        builder.HasIndex(u => u.CodigoCadastro).IsUnique();
        builder.Property(u => u.PinConfigurado).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.NumeroChamada);
        
        builder.Property(u => u.Tipo)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(u => u.AdministradorGeral)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(u => u.Ativo)
            .IsRequired();
        
        builder.Property(u => u.DataCriacao)
            .IsRequired();
        
        builder.Property(u => u.DataUltimoAcesso);
    }
}
