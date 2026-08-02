using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SiPacul.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data;

public sealed class SiPaculDbContext :
    IdentityUserContext<ApplicationUser, Guid>
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

    public DbSet<Sale> Sales =>
        Set<Sale>();

    public DbSet<SaleLine> SaleLines =>
        Set<SaleLine>();

    public DbSet<CultivationExpense> CultivationExpenses =>
        Set<CultivationExpense>();

    public DbSet<CapitalContribution> CapitalContributions =>
        Set<CapitalContribution>();

    public DbSet<SalePayment> SalePayments =>
        Set<SalePayment>();

    public DbSet<ProfitSharingSettlement>
        ProfitSharingSettlements =>
        Set<ProfitSharingSettlement>();

    public DbSet<ProfitSharingAllocation>
        ProfitSharingAllocations =>
        Set<ProfitSharingAllocation>();

    public DbSet<Land> Lands =>
        Set<Land>();

    public DbSet<LandPlot> LandPlots =>
        Set<LandPlot>();

    public DbSet<ApplicationUser> ApplicationUsers =>
        Set<ApplicationUser>();

    public DbSet<OrganizationMembership>
        OrganizationMemberships =>
            Set<OrganizationMembership>();

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        EnsureProfitSharingSourcesAreUnlocked();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(
            true,
            cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await EnsureProfitSharingSourcesAreUnlockedAsync(
            cancellationToken);

        return await base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void EnsureProfitSharingSourcesAreUnlocked()
    {
        foreach (var source in GetExpenseSources())
        {
            ThrowWhenCycleIsLocked(
                source.OrganizationId,
                source.CropCycleId,
                "CultivationExpenses.FinalizedSettlementExists",
                "cultivation expense");
        }

        foreach (var source in GetCapitalSources())
        {
            ThrowWhenCycleIsLocked(
                source.OrganizationId,
                source.CropCycleId,
                "CapitalContributions.FinalizedSettlementExists",
                "capital contribution");
        }

        foreach (var group in GetPaymentSources()
            .GroupBy(source => source.OrganizationId))
        {
            var saleIds =
                group
                    .Select(source => source.SaleId)
                    .Distinct()
                    .ToArray();

            var cropCycleIds =
                Set<SaleLine>()
                    .AsNoTracking()
                    .Where(line =>
                        line.OrganizationId == group.Key &&
                        saleIds.Contains(line.SaleId))
                    .Select(line =>
                        line.CropCycleIdSnapshot)
                    .Distinct()
                    .ToArray();

            foreach (var cropCycleId in cropCycleIds)
            {
                ThrowWhenCycleIsLocked(
                    group.Key,
                    cropCycleId,
                    "SalePayments.FinalizedSettlementExists",
                    "sale payment");
            }
        }
    }

    private async Task
        EnsureProfitSharingSourcesAreUnlockedAsync(
            CancellationToken cancellationToken)
    {
        foreach (var source in GetExpenseSources())
        {
            await ThrowWhenCycleIsLockedAsync(
                source.OrganizationId,
                source.CropCycleId,
                "CultivationExpenses.FinalizedSettlementExists",
                "cultivation expense",
                cancellationToken);
        }

        foreach (var source in GetCapitalSources())
        {
            await ThrowWhenCycleIsLockedAsync(
                source.OrganizationId,
                source.CropCycleId,
                "CapitalContributions.FinalizedSettlementExists",
                "capital contribution",
                cancellationToken);
        }

        foreach (var group in GetPaymentSources()
            .GroupBy(source => source.OrganizationId))
        {
            var saleIds =
                group
                    .Select(source => source.SaleId)
                    .Distinct()
                    .ToArray();

            var cropCycleIds =
                await Set<SaleLine>()
                    .AsNoTracking()
                    .Where(line =>
                        line.OrganizationId == group.Key &&
                        saleIds.Contains(line.SaleId))
                    .Select(line =>
                        line.CropCycleIdSnapshot)
                    .Distinct()
                    .ToArrayAsync(cancellationToken);

            foreach (var cropCycleId in cropCycleIds)
            {
                await ThrowWhenCycleIsLockedAsync(
                    group.Key,
                    cropCycleId,
                    "SalePayments.FinalizedSettlementExists",
                    "sale payment",
                    cancellationToken);
            }
        }
    }

    private IReadOnlyList<CycleSource> GetExpenseSources()
    {
        return ChangeTracker
            .Entries<CultivationExpense>()
            .Where(entry => IsSourceMutation(entry.State))
            .Select(entry =>
                new CycleSource(
                    entry.Entity.OrganizationId,
                    entry.Entity.CropCycleId))
            .Distinct()
            .ToArray();
    }

    private IReadOnlyList<CycleSource> GetCapitalSources()
    {
        return ChangeTracker
            .Entries<CapitalContribution>()
            .Where(entry => IsSourceMutation(entry.State))
            .Select(entry =>
                new CycleSource(
                    entry.Entity.OrganizationId,
                    entry.Entity.CropCycleId))
            .Distinct()
            .ToArray();
    }

    private IReadOnlyList<SaleSource> GetPaymentSources()
    {
        return ChangeTracker
            .Entries<SalePayment>()
            .Where(entry => IsSourceMutation(entry.State))
            .Select(entry =>
                new SaleSource(
                    entry.Entity.OrganizationId,
                    entry.Entity.SaleId))
            .Distinct()
            .ToArray();
    }

    private void ThrowWhenCycleIsLocked(
        Guid organizationId,
        Guid cropCycleId,
        string errorCode,
        string sourceType)
    {
        var settlement =
            Set<ProfitSharingSettlement>()
                .AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId ==
                        organizationId &&
                    candidate.CropCycleId ==
                        cropCycleId &&
                    candidate.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !candidate.IsDeleted)
                .Select(candidate =>
                    new
                    {
                        candidate.Id,
                        candidate.CropCycleId
                    })
                .FirstOrDefault();

        if (settlement is null)
        {
            return;
        }

        throw new ProfitSharingSourceLockedException(
            errorCode,
            sourceType,
            organizationId,
            settlement.CropCycleId,
            settlement.Id);
    }

    private async Task ThrowWhenCycleIsLockedAsync(
        Guid organizationId,
        Guid cropCycleId,
        string errorCode,
        string sourceType,
        CancellationToken cancellationToken)
    {
        var settlement =
            await Set<ProfitSharingSettlement>()
                .AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId ==
                        organizationId &&
                    candidate.CropCycleId ==
                        cropCycleId &&
                    candidate.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !candidate.IsDeleted)
                .Select(candidate =>
                    new
                    {
                        candidate.Id,
                        candidate.CropCycleId
                    })
                .FirstOrDefaultAsync(cancellationToken);

        if (settlement is null)
        {
            return;
        }

        throw new ProfitSharingSourceLockedException(
            errorCode,
            sourceType,
            organizationId,
            settlement.CropCycleId,
            settlement.Id);
    }

    private static bool IsSourceMutation(
        EntityState state)
    {
        return state is
            EntityState.Added or
            EntityState.Modified or
            EntityState.Deleted;
    }

    private sealed record CycleSource(
        Guid OrganizationId,
        Guid CropCycleId);

    private sealed record SaleSource(
        Guid OrganizationId,
        Guid SaleId);

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims");

        modelBuilder
            .Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins");

        modelBuilder
            .Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens");


        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SiPaculDbContext).Assembly);
    }
}
