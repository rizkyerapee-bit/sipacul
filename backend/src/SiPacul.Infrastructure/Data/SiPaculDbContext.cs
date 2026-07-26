using Microsoft.EntityFrameworkCore;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Infrastructure.Data;

public sealed class SiPaculDbContext : DbContext
{
    public SiPaculDbContext(
        DbContextOptions<SiPaculDbContext> options)
        : base(options)
    {
    }

    public DbSet<Commodity> Commodities => Set<Commodity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SiPaculDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
