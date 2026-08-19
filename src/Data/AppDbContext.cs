using Microsoft.EntityFrameworkCore;
using SmartUrlShortener.Models;

namespace SmartUrlShortener.Data;

public class AppDbContext : DbContext 
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<UrlMapping> UrlMappings => Set<UrlMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UrlMapping>()
            .HasIndex(u => u.ShortCode)
            .IsUnique();
    }
}