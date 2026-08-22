using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Evaluations.SeasonReviews;
using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Application.Evaluations.SeasonReviews.Persistence;
using SiPacul.Application.Evaluations.SeasonReviews.Services;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Tests.Evaluations.SeasonReviews;

public sealed class SeasonReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_ForCompletedCycle_PersistsDraft()
    {
        var context = Context(CompletedCycle());
        var result = await context.Service.CreateAsync(context.OrganizationId, Request(context.Cycle.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(SeasonReviewStatus.Draft, result.Value.Status);
        Assert.Equal(context.Cycle.Id, result.Value.CropCycleId);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.NotNull(context.Reviews.Stored);
    }

    [Fact]
    public async Task CreateAsync_ForPlannedCycle_ReturnsConflictWithoutSave()
    {
        var context = Context(NewCycle());
        var result = await context.Service.CreateAsync(context.OrganizationId, Request(context.Cycle.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(SeasonReviewErrors.CropCycleNotTerminalCode, result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_WhenReviewExists_ReturnsConflict()
    {
        var cycle = CompletedCycle();
        var context = Context(cycle);
        context.Reviews.Stored = Review(context.OrganizationId, cycle.Id);

        var result = await context.Service.CreateAsync(context.OrganizationId, Request(cycle.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(SeasonReviewErrors.AlreadyExistsCode, result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_CannotReadCycleFromAnotherOrganization()
    {
        var context = Context(CompletedCycle());
        var result = await context.Service.CreateAsync(Guid.NewGuid(), Request(context.Cycle.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(SeasonReviewErrors.CropCycleNotFoundCode, result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ChangesDraftAndSaves()
    {
        var cycle = CompletedCycle();
        var context = Context(cycle);
        var review = Review(context.OrganizationId, cycle.Id);
        context.Reviews.Stored = review;
        var request = new UpdateSeasonReviewRequest(new DateOnly(2026, 8, 22), "Updated findings", "Updated lessons", "Updated recommendations");

        var result = await context.Service.UpdateAsync(context.OrganizationId, review.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated findings", result.Value.Findings);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task FinalizeAsync_FinalizesAndSaves()
    {
        var cycle = CompletedCycle();
        var context = Context(cycle);
        var review = Review(context.OrganizationId, cycle.Id);
        context.Reviews.Stored = review;

        var result = await context.Service.FinalizeAsync(context.OrganizationId, review.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(SeasonReviewStatus.Finalized, result.Value.Status);
        Assert.NotNull(result.Value.FinalizedAt);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_AfterFinalize_ReturnsConflictWithoutSave()
    {
        var cycle = CompletedCycle();
        var context = Context(cycle);
        var review = Review(context.OrganizationId, cycle.Id);
        review.FinalizeReview();
        context.Reviews.Stored = review;

        var result = await context.Service.UpdateAsync(context.OrganizationId, review.Id,
            new UpdateSeasonReviewRequest(new DateOnly(2026, 8, 22), "Changed", "Changed", "Changed"));

        Assert.True(result.IsFailure);
        Assert.Equal(SeasonReviewErrors.InvalidStatusTransitionCode, result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherOrganization_ReturnsNotFound()
    {
        var cycle = CompletedCycle();
        var context = Context(cycle);
        var review = Review(context.OrganizationId, cycle.Id);
        context.Reviews.Stored = review;

        var result = await context.Service.GetByIdAsync(Guid.NewGuid(), review.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(SeasonReviewErrors.NotFoundCode, result.Error.Code);
    }

    private static TestContext Context(CropCycle cycle)
    {
        var reviews = new FakeReviewRepository();
        var unitOfWork = new FakeUnitOfWork();
        return new TestContext(cycle.OrganizationId, cycle, reviews, unitOfWork,
            new SeasonReviewService(reviews, new FakeCropCycleRepository(cycle), unitOfWork));
    }

    private static CreateSeasonReviewRequest Request(Guid cycleId) =>
        new(cycleId, new DateOnly(2026, 8, 22), "Findings", "Lessons", "Recommendations");

    private static SeasonReview Review(Guid organizationId, Guid cycleId) =>
        SeasonReview.Create(organizationId, cycleId, new DateOnly(2026, 8, 22), "Findings", "Lessons", "Recommendations");

    private static CropCycle NewCycle()
    {
        var start = new DateOnly(2026, 1, 1);
        return CropCycle.Create(Guid.NewGuid(), "CYCLE-001", "Season", Guid.NewGuid(), null,
            Guid.NewGuid(), Guid.NewGuid(), 1m, AreaUnit.Hectare, start, start.AddMonths(4), null);
    }

    private static CropCycle CompletedCycle()
    {
        var cycle = NewCycle();
        cycle.Start(new DateOnly(2026, 1, 1));
        cycle.Complete(new DateOnly(2026, 5, 1));
        return cycle;
    }

    private sealed record TestContext(Guid OrganizationId, CropCycle Cycle, FakeReviewRepository Reviews,
        FakeUnitOfWork UnitOfWork, SeasonReviewService Service);

    private sealed class FakeReviewRepository : ISeasonReviewRepository
    {
        public SeasonReview? Stored { get; set; }
        public Task<SeasonReview?> GetByIdAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored is not null && Stored.OrganizationId == organizationId && Stored.Id == reviewId ? Stored : null);
        public Task<SeasonReview?> GetByCropCycleAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored is not null && Stored.OrganizationId == organizationId && Stored.CropCycleId == cropCycleId ? Stored : null);
        public Task AddAsync(SeasonReview review, CancellationToken cancellationToken = default) { Stored = review; return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }

    private sealed class FakeCropCycleRepository(CropCycle cycle) : ICropCycleRepository
    {
        public Task<CropCycle?> GetByIdAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(cycle.OrganizationId == organizationId && cycle.Id == cropCycleId ? cycle : null);
        public Task<CropCycle?> GetByIdForUpdateAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken = default) => GetByIdAsync(organizationId, cropCycleId, cancellationToken);
        public Task<IReadOnlyList<CropCycle>> GetAllAsync(Guid organizationId, CropCycleStatus? status = null, Guid? commodityId = null, Guid? landId = null, Guid? landPlotId = null, DateOnly? plannedStartFrom = null, DateOnly? plannedStartTo = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CropCycle>>([]);
        public Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasScheduleConflictAsync(Guid organizationId, Guid landId, Guid landPlotId, DateOnly plannedStartDate, DateOnly expectedHarvestDate, Guid? excludedCropCycleId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasInProgressCycleAsync(Guid organizationId, Guid landId, Guid landPlotId, Guid? excludedCropCycleId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasActiveCycleForLandAsync(Guid organizationId, Guid landId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasActiveCycleForPlotAsync(Guid organizationId, Guid landId, Guid landPlotId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasAnyCycleForPlotAsync(Guid organizationId, Guid landId, Guid landPlotId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Add(CropCycle cropCycle) { }
    }
}
