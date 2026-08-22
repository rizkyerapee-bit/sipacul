using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Evaluations.SeasonReviews.Persistence;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class SeasonReviewRepositoryTests
{
    private static readonly Guid OrganizationId = Guid.Parse(
        "10000000-0000-0000-0000-000000000001");

    private static readonly Guid OtherOrganizationId = Guid.Parse(
        "10000000-0000-0000-0000-000000000002");

    private static readonly Guid CropCycleId = Guid.Parse(
        "20000000-0000-0000-0000-000000000001");

    [Fact]
    public void Repository_ShouldImplementContractAndBeSealed()
    {
        using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);

        Assert.IsAssignableFrom<ISeasonReviewRepository>(repository);
        Assert.True(typeof(SeasonReviewRepository).IsSealed);
    }

    [Fact]
    public void Constructor_NullContext_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SeasonReviewRepository(null!));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepositoryAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;" +
                        "Database=sipacul_tests;" +
                        "Username=sipacul;Password=sipacul"
                })
            .Build();

        services.AddInfrastructure(configuration);

        var descriptor = services.Single(service =>
            service.ServiceType ==
                typeof(ISeasonReviewRepository));

        Assert.Equal(
            typeof(SeasonReviewRepository),
            descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public async Task AddAndGet_ShouldRespectOrganizationAndCycle()
    {
        await using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);
        var review = CreateReview();

        await repository.AddAsync(review);
        await context.SaveChangesAsync();

        Assert.Same(
            review,
            await repository.GetByIdAsync(
                OrganizationId,
                review.Id));
        Assert.Same(
            review,
            await repository.GetByCropCycleAsync(
                OrganizationId,
                CropCycleId));
        Assert.Null(
            await repository.GetByIdAsync(
                OtherOrganizationId,
                review.Id));
        Assert.Null(
            await repository.GetByCropCycleAsync(
                OtherOrganizationId,
                CropCycleId));
    }

    [Fact]
    public async Task Reads_ShouldExcludeSoftDeletedReview()
    {
        await using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);
        var review = CreateReview();
        review.SoftDelete("reviewer@example.test");

        await repository.AddAsync(review);
        await context.SaveChangesAsync();

        Assert.Null(
            await repository.GetByIdAsync(
                OrganizationId,
                review.Id));
        Assert.Null(
            await repository.GetByCropCycleAsync(
                OrganizationId,
                CropCycleId));
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("review")]
    public async Task GetById_EmptyIdentifier_ShouldThrow(
        string identifier)
    {
        await using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetByIdAsync(
                identifier == "organization"
                    ? Guid.Empty
                    : OrganizationId,
                identifier == "review"
                    ? Guid.Empty
                    : Guid.NewGuid()));

        Assert.Equal(
            identifier == "organization"
                ? "organizationId"
                : "reviewId",
            exception.ParamName);
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("cycle")]
    public async Task GetByCropCycle_EmptyIdentifier_ShouldThrow(
        string identifier)
    {
        await using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetByCropCycleAsync(
                identifier == "organization"
                    ? Guid.Empty
                    : OrganizationId,
                identifier == "cycle"
                    ? Guid.Empty
                    : CropCycleId));

        Assert.Equal(
            identifier == "organization"
                ? "organizationId"
                : "cropCycleId",
            exception.ParamName);
    }

    [Fact]
    public async Task Add_NullReview_ShouldThrow()
    {
        await using var context = CreateContext();
        var repository = new SeasonReviewRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.AddAsync(null!));
    }

    private static SeasonReview CreateReview()
    {
        return SeasonReview.Create(
            OrganizationId,
            CropCycleId,
            new DateOnly(2027, 5, 20),
            "Temuan musim",
            "Pelajaran musim",
            "Rekomendasi musim berikutnya");
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseInMemoryDatabase(
                    "season-review-" + Guid.NewGuid())
                .Options;

        return new SiPaculDbContext(options);
    }
}
