using ColetaCerta.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ColetaCerta.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PontoColeta> PontosColeta => Set<PontoColeta>();

    public DbSet<Lixeira> Lixeiras => Set<Lixeira>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PontoColeta>(entity =>
        {
            entity.ToTable("pontos_coleta");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Nome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Endereco)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Bairro)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Cidade)
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<Lixeira>(entity =>
        {
            entity.ToTable("lixeiras");

            entity.HasKey(l => l.Id);

            entity.Property(l => l.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(l => l.Codigo)
                .IsUnique();

            entity.Property(l => l.CapacidadeLitros)
                .IsRequired();

            entity.HasOne(l => l.PontoColeta)
                .WithMany(p => p.Lixeiras)
                .HasForeignKey(l => l.PontoColetaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Login)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(u => u.Login)
                .IsUnique();

            entity.Property(u => u.SenhaHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(u => u.Perfil)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(u => u.Ativo)
                .IsRequired();
        });
    }
}