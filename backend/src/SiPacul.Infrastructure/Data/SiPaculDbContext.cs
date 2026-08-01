using Microsoft.EntityFrameworkCore;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data;

public sealed class SiPaculDbContext : DbContext
{
    public SiPaculDbContext(
        DbContextOptions<SiPaculDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations =>
        Set<Organization>();

    public DbSet<CommodityCategory> CommodityCategories =>
        Set<CommodityCategory>();

    public DbSet<Commodity> Commodities =>
        Set<Commodity>();

    public DbSet<CultivationSop> CultivationSops =>
        Set<CultivationSop>();

    public DbSet<CultivationSopStep> CultivationSopSteps =>
        Set<CultivationSopStep>();

    public DbSet<CropCycle> CropCycles =>
        Set<CropCycle>();

    public DbSet<CultivationActivity>
        CultivationActivities =>
        Set<CultivationActivity>();

    public DbSet<CultivationActivityResource>
        CultivationActivityResources =>
        Set<CultivationActivityResource>();

    public DbSet<HarvestBatch> HarvestBatches =>
        Set<HarvestBatch>();

    public DbSet<Land> Lands =>
        Set<Land>();

    public DbSet<LandPlot> LandPlots =>
        Set<LandPlot>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SiPaculDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
