using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Previews;
using SiPacul.Application.Finance.ProfitSharing.Previews.Services;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Finance.ProfitSharing.Previews;

public sealed class ProfitSharingPreviewServiceTests
{
    private static readonly DateTime GeneratedAt =
        new(2027, 7, 24, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Get_CompanyAndPartner_ShouldCalculateWaterfall()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                80_000m),
            CreateContribution(
                context,
                "MOD-PARTNER",
                "MITRA",
                "Mitra Tani",
                CapitalContributorRole.Partner,
                20_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 80_000m, 20_000m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPersisted);
        Assert.Equal("SIPACUL-PS-2", result.Value.CalculationVersion);
        Assert.Equal(150_000m, result.Value.Totals.TotalPayout);
        Assert.Equal(50_000m, result.Value.Totals.TotalProfitShare);
        Assert.Equal(GeneratedAt, result.Value.GeneratedAt);
        Assert.Equal(assignment.Id, result.Value.SchemeSnapshot.Id);

        var partner = Assert.Single(
            result.Value.Allocations,
            allocation => allocation.ParticipantCodeSnapshot == "MITRA");
        Assert.Equal(16_666.67m, partner.ManagementProfitShare);
        Assert.Equal(6_666.67m, partner.ResidualProfitShare);
        Assert.Equal(43_333.34m, partner.TotalPayout);
    }

    [Fact]
    public async Task Get_WithPassiveInvestor_ShouldApplyPriorityReturn()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId,
            includePassiveInvestor: true);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                60_000m),
            CreateContribution(
                context,
                "MOD-PARTNER",
                "MITRA",
                "Mitra Tani",
                CapitalContributorRole.Partner,
                20_000m),
            CreateContribution(
                context,
                "MOD-PASSIVE",
                "INVESTOR",
                "Investor Pasif",
                CapitalContributorRole.Investor,
                20_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 80_000m, 20_000m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsSuccess);
        var passive = Assert.Single(
            result.Value.Allocations,
            allocation => allocation.ParticipantCodeSnapshot == "INVESTOR");
        Assert.Equal(2_000m, passive.ReturnOnCapitalProfitShare);
        Assert.Equal(0m, passive.ResidualProfitShare);
        Assert.Equal(22_000m, passive.TotalPayout);
        Assert.Equal(2, result.Value.PriorityAllocations.Count);
    }

    [Fact]
    public async Task Get_MultipleConfirmedDeposits_ShouldAggregateByCode()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY-1",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                30_000m),
            CreateContribution(
                context,
                "MOD-COMPANY-2",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                50_000m),
            CreateContribution(
                context,
                "MOD-PARTNER",
                "MITRA",
                "Mitra Tani",
                CapitalContributorRole.Partner,
                20_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 80_000m, 20_000m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsSuccess);
        var company = Assert.Single(
            result.Value.Allocations,
            allocation => allocation.ParticipantCodeSnapshot == "PERUSAHAAN");
        Assert.Equal(80_000m, company.ConfirmedCapital);
        Assert.Equal(CapitalContributionStatus.Confirmed, capital.LastStatus);
    }

    [Fact]
    public async Task Get_WithoutAssignment_ShouldReturnNotFound()
    {
        var context = CreateContext();
        var service = CreateService(
            context,
            null,
            new FakeCapitalContributionRepository(),
            CreateSnapshot(context, 0m, 0m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.AssignmentNotFoundCode,
            result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Get_UnknownCapitalCode_ShouldReturnConflict()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-UNKNOWN",
                "UNKNOWN",
                "Pemodal Lain",
                CapitalContributorRole.Investor,
                100_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 100_000m, 0m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.CapitalNotInSchemeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_MismatchedCapitalRole_ShouldReturnConflict()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Partner,
                100_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 0m, 100_000m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.CapitalRoleMismatchCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_InconsistentContributorName_ShouldReturnConflict()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY-1",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                50_000m),
            CreateContribution(
                context,
                "MOD-COMPANY-2",
                "PERUSAHAAN",
                "Nama Berubah",
                CapitalContributorRole.Investor,
                50_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 100_000m, 0m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.CapitalIdentityConflictCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_WhenCapitalChangesBetweenReads_ShouldConflict()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                90_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 100_000m, 0m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.SourceDataChangedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_WithFundingGap_ShouldReturnCalculationConflict()
    {
        var context = CreateContext();
        var assignment = CreateAssignment(
            context.Organization.Id,
            context.CropCycleId);
        var capital = new FakeCapitalContributionRepository(
            CreateContribution(
                context,
                "MOD-COMPANY",
                "PERUSAHAAN",
                "Perusahaan",
                CapitalContributorRole.Investor,
                80_000m));
        var service = CreateService(
            context,
            assignment,
            capital,
            CreateSnapshot(context, 80_000m, 0m));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.CalculationUnavailableCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_WithEmptyOrganizationId_ShouldReturnValidation()
    {
        var context = CreateContext();
        var service = CreateService(
            context,
            null,
            new FakeCapitalContributionRepository(),
            null);

        var result = await service.GetAsync(
            Guid.Empty,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.ValidationCode,
            result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Get_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var context = CreateContext();
        var service = new ProfitSharingPreviewService(
            new FakeAssignmentRepository(),
            new FakeProfitabilityRepository(),
            new FakeCapitalContributionRepository(),
            new FakeOrganizationRepository(),
            new FixedTimeProvider(GeneratedAt));

        var result = await service.GetAsync(
            context.Organization.Id,
            context.CropCycleId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingPreviewErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    private static ProfitSharingPreviewService CreateService(
        TestContext context,
        ProfitSharingSchemeAssignment? assignment,
        FakeCapitalContributionRepository capitalRepository,
        ProfitabilitySourceSnapshot? snapshot)
    {
        return new ProfitSharingPreviewService(
            new FakeAssignmentRepository(assignment),
            new FakeProfitabilityRepository(snapshot),
            capitalRepository,
            new FakeOrganizationRepository(context.Organization),
            new FixedTimeProvider(GeneratedAt));
    }

    private static TestContext CreateContext()
    {
        return new TestContext(
            Organization.Create("ORG-001", "Organisasi Uji"),
            Guid.NewGuid());
    }

    private static ProfitabilitySourceSnapshot CreateSnapshot(
        TestContext context,
        decimal investorCapital,
        decimal partnerCapital)
    {
        return new ProfitabilitySourceSnapshot(
            context.Organization.Id,
            context.CropCycleId,
            "CYCLE-001",
            "Siklus Uji",
            Guid.NewGuid(),
            "CABAI",
            "Cabai",
            150_000m,
            150_000m,
            100_000m,
            0m,
            investorCapital,
            partnerCapital,
            0m,
            HarvestQuantityUnit.Kilogram);
    }

    private static ProfitSharingSchemeAssignment CreateAssignment(
        Guid organizationId,
        Guid cropCycleId,
        bool includePassiveInvestor = false)
    {
        var participants = new List<
            ProfitSharingSchemeParticipantDefinition>
        {
            new(
                "PERUSAHAAN",
                "Perusahaan",
                ProfitSharingParticipantRole.Company,
                true,
                1),
            new(
                "MITRA",
                "Mitra Tani",
                ProfitSharingParticipantRole.ManagingPartner,
                true,
                2)
        };

        var rules = new List<
            ProfitSharingSchemePriorityRuleDefinition>
        {
            new(
                "BIAYA-KELOLA",
                ProfitSharingPriorityRuleType.ManagementShare,
                "MITRA",
                ProfitSharingRate.FromFraction(1m, 3m),
                1)
        };

        if (includePassiveInvestor)
        {
            participants.Add(
                new ProfitSharingSchemeParticipantDefinition(
                    "INVESTOR",
                    "Investor Pasif",
                    ProfitSharingParticipantRole.PassiveInvestor,
                    false,
                    3));
            rules.Add(
                new ProfitSharingSchemePriorityRuleDefinition(
                    "IMBAL-INVESTOR",
                    ProfitSharingPriorityRuleType.ReturnOnCapital,
                    "INVESTOR",
                    ProfitSharingRate.FromPercentage(10m),
                    2));
        }

        var scheme = ProfitSharingScheme.CreateDraft(
            organizationId,
            "SCHEME-001",
            "Skema Uji",
            null,
            participants,
            rules,
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            []);
        scheme.Activate();

        return ProfitSharingSchemeAssignment.Create(
            organizationId,
            cropCycleId,
            scheme);
    }

    private static CapitalContribution CreateContribution(
        TestContext context,
        string transactionCode,
        string contributorCode,
        string contributorName,
        CapitalContributorRole role,
        decimal amount)
    {
        var contribution = CapitalContribution.Create(
            context.Organization.Id,
            context.CropCycleId,
            transactionCode,
            new DateOnly(2027, 1, 1),
            contributorCode,
            contributorName,
            role,
            amount,
            CapitalContributionPaymentMethod.BankTransfer,
            null,
            null);
        contribution.Confirm();
        return contribution;
    }

    private sealed record TestContext(
        Organization Organization,
        Guid CropCycleId);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class FakeProfitabilityRepository :
        IProfitabilityReadRepository
    {
        private readonly ProfitabilitySourceSnapshot? _snapshot;

        public FakeProfitabilityRepository(
            ProfitabilitySourceSnapshot? snapshot = null)
        {
            _snapshot = snapshot;
        }

        public Task<ProfitabilitySourceSnapshot?> GetAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _snapshot?.OrganizationId == organizationId &&
                _snapshot.CropCycleId == cropCycleId
                    ? _snapshot
                    : null);
        }
    }

    private sealed class FakeAssignmentRepository :
        IProfitSharingSchemeAssignmentRepository
    {
        private readonly ProfitSharingSchemeAssignment? _assignment;

        public FakeAssignmentRepository(
            ProfitSharingSchemeAssignment? assignment = null)
        {
            _assignment = assignment;
        }

        public Task<ProfitSharingSchemeAssignment?> GetByCropCycleAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _assignment?.OrganizationId == organizationId &&
                _assignment.CropCycleId == cropCycleId
                    ? _assignment
                    : null);
        }

        public Task<ProfitSharingSchemeAssignment?>
            GetByCropCycleForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return GetByCropCycleAsync(
                organizationId,
                cropCycleId,
                cancellationToken);
        }

        public void Add(ProfitSharingSchemeAssignment assignment)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeCapitalContributionRepository :
        ICapitalContributionRepository
    {
        private readonly List<CapitalContribution> _contributions = [];

        public FakeCapitalContributionRepository(
            params CapitalContribution[] contributions)
        {
            _contributions.AddRange(contributions);
        }

        public CapitalContributionStatus? LastStatus { get; private set; }

        public Task<IReadOnlyList<CapitalContribution>> GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CapitalContributionStatus? status = null,
            CapitalContributorRole? contributorRole = null,
            DateOnly? contributionDateFrom = null,
            DateOnly? contributionDateTo = null,
            string? contributorCode = null,
            string? contributorName = null,
            CancellationToken cancellationToken = default)
        {
            LastStatus = status;
            IReadOnlyList<CapitalContribution> result = _contributions
                .Where(contribution =>
                    contribution.OrganizationId == organizationId &&
                    contribution.CropCycleId == cropCycleId &&
                    (!status.HasValue ||
                     contribution.Status == status.Value) &&
                    (!contributorRole.HasValue ||
                     contribution.ContributorRole == contributorRole))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<CapitalContribution?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CapitalContribution?>(null);

        public Task<CapitalContribution?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CapitalContribution?>(null);

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<CapitalContribution?> GetContributorIdentityAsync(
            Guid organizationId,
            CapitalContributorRole contributorRole,
            string contributorCode,
            Guid? excludedContributionId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CapitalContribution?>(null);

        public Task<CapitalContribution?> GetPartnerIdentityAsync(
            Guid organizationId,
            Guid? excludedContributionId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CapitalContribution?>(null);

        public void Add(CapitalContribution contribution)
        {
            _contributions.Add(contribution);
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        private readonly Organization? _organization;

        public FakeOrganizationRepository(
            Organization? organization = null)
        {
            _organization = organization;
        }

        public Task<IReadOnlyList<Organization>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Organization> result =
                _organization is null ? [] : [_organization];
            return Task.FromResult(result);
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _organization?.Id == organizationId
                    ? _organization
                    : null);

        public Task<Organization?> GetByIdForUpdateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(organizationId, cancellationToken);

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_organization?.Code == code);

        public void Add(Organization organization)
        {
            throw new NotSupportedException();
        }
    }
}
