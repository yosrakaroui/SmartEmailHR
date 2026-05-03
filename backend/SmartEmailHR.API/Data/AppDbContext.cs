using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Offre> Offres => Set<Offre>();
    public DbSet<Candidature> Candidatures => Set<Candidature>();
    public DbSet<AnalyseIA> AnalysesIA => Set<AnalyseIA>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Offre>()
            .HasIndex(o => new { o.Domaine, o.Statut });

        modelBuilder.Entity<Candidature>()
            .HasIndex(c => new { c.OffreId, c.Statut });

        modelBuilder.Entity<Candidature>()
            .HasIndex(c => c.EmailCandidat);

        modelBuilder.Entity<AnalyseIA>()
            .HasIndex(a => a.Score);

        modelBuilder.Entity<User>()
            .HasMany(u => u.OffresCreees)
            .WithOne(o => o.Createur)
            .HasForeignKey(o => o.CreePar)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Offre>()
            .HasMany(o => o.Candidatures)
            .WithOne(c => c.Offre)
            .HasForeignKey(c => c.OffreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Candidature>()
            .HasOne(c => c.AnalyseIA)
            .WithOne(a => a.Candidature)
            .HasForeignKey<AnalyseIA>(a => a.CandidatureId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Candidature>()
            .HasMany(c => c.EmailLogs)
            .WithOne(e => e.Candidature)
            .HasForeignKey(e => e.CandidatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
