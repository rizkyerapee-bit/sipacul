using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Services;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Finance.ProfitSharing.Assignments;

public sealed class ProfitSharingSchemeAssignmentServiceTests
{
    [Fact]
    public async Task Assign_PlannedCycleAndActiveScheme_ShouldSave()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var scheme = CreateScheme(organization.Id, active: true);
        var assignments = new FakeAssignmentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            assignments,
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(scheme.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(scheme.Id, result.Value.SourceSchemeId);
        Assert.Equal(3, result.Value.Participants.Count);
        Assert.Single(assignments.Assignments);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_SameScheme_ShouldBeIdempotent()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var scheme = CreateScheme(organization.Id, active: true);
        var existing = ProfitSharingSchemeAssignment.Create(
            organization.Id,
            cropCycle.Id,
            scheme);
        scheme.Supersede();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            new FakeAssignmentRepository(existing),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(scheme.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value.Id);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_ReplacementWhilePlanned_ShouldReplace()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var first = CreateScheme(
            organization.Id,
            active: true,
            code: "SCHEME-ONE");
        var replacement = CreateScheme(
            organization.Id,
            active: true,
            code: "SCHEME-TWO");
        var existing = ProfitSharingSchemeAssignment.Create(
            organization.Id,
            cropCycle.Id,
            first);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(first, replacement),
            new FakeAssignmentRepository(existing),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(replacement.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(replacement.Id, result.Value.SourceSchemeId);
        Assert.Equal("SCHEME-TWO", result.Value.SchemeCode);
        Assert.NotNull(result.Value.UpdatedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_ReplacementAfterStart_ShouldConflict()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        cropCycle.Start(cropCycle.PlannedStartDate);
        var first = CreateScheme(
            organization.Id,
            active: true,
            code: "SCHEME-ONE");
        var replacement = CreateScheme(
            organization.Id,
            active: true,
            code: "SCHEME-TWO");
        var existing = ProfitSharingSchemeAssignment.Create(
            organization.Id,
            cropCycle.Id,
            first);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(first, replacement),
            new FakeAssignmentRepository(existing),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(replacement.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors
                .AssignmentLockedCode,
            result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_FirstAssignmentAfterStart_ShouldBeAllowed()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        cropCycle.Start(cropCycle.PlannedStartDate);
        var scheme = CreateScheme(organization.Id, active: true);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            new FakeAssignmentRepository(),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(scheme.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_DraftScheme_ShouldConflict()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var scheme = CreateScheme(organization.Id, active: false);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            new FakeAssignmentRepository(),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(scheme.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors
                .SchemeNotActiveCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_CompletedCycle_ShouldConflict()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        cropCycle.Start(cropCycle.PlannedStartDate);
        cropCycle.Complete(cropCycle.ExpectedHarvestDate);
        var scheme = CreateScheme(organization.Id, active: true);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            new FakeAssignmentRepository(),
            unitOfWork);

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(scheme.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors
                .CropCycleClosedCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_WhenSchemeMissing_ShouldNotFound()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(),
            new FakeAssignmentRepository(),
            new FakeUnitOfWork());

        var result = await service.AssignAsync(
            organization.Id,
            cropCycle.Id,
            new AssignProfitSharingSchemeRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors.SchemeNotFoundCode,
            result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Get_WithAssignment_ShouldReturnSnapshot()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var scheme = CreateScheme(organization.Id, active: true);
        var assignment = ProfitSharingSchemeAssignment.Create(
            organization.Id,
            cropCycle.Id,
            scheme);
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(scheme),
            new FakeAssignmentRepository(assignment),
            new FakeUnitOfWork());

        var result = await service.GetAsync(
            organization.Id,
            cropCycle.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(assignment.Id, result.Value.Id);
        Assert.Equal("INVESTOR", result.Value.Participants[2]
            .ParticipantCode);
    }

    [Fact]
    public async Task Get_WithoutAssignment_ShouldNotFound()
    {
        var organization = CreateOrganization();
        var cropCycle = CreateCropCycle(organization.Id);
        var service = CreateService(
            organization,
            cropCycle,
            new FakeSchemeRepository(),
            new FakeAssignmentRepository(),
            new FakeUnitOfWork());

        var result = await service.GetAsync(
            organization.Id,
            cropCycle.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors
                .AssignmentNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Assign_WhenOrganizationMissing_ShouldNotFound()
    {
        var service = new ProfitSharingSchemeAssignmentService(
            new FakeAssignmentRepository(),
            new FakeSchemeRepository(),
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AssignProfitSharingSchemeRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeAssignmentErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    private static ProfitSharingSchemeAssignmentService
        CreateService(
            Organization organization,
            CropCycle cropCycle,
            FakeSchemeRepository schemeRepository,
            FakeAssignmentRepository assignmentRepository,
            FakeUnitOfWork unitOfWork)
    {
        return new ProfitSharingSchemeAssignmentService(
            assignmentRepository,
            schemeRepository,
            new FakeCropCycleRepository(cropCycle),
            new FakeOrganizationRepository(organization),
            unitOfWork);
    }

    private static Organization CreateOrganization()
    {
        return Organization.Create(
            "ORG-001",
            "Organisasi Uji");
    }

    private static CropCycle CreateCropCycle(Guid organizationId)
    {
        return CropCycle.Create(
            organizationId,
            "CYCLE-001",
            "Siklus Uji",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            AreaUnit.Hectare,
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 6, 30),
            null);
    }

    private static ProfitSharingScheme CreateScheme(
        Guid organizationId,
        bool active,
        string code = "SCHEME-001")
    {
        var scheme = ProfitSharingScheme.CreateDraft(
            organizationId,
            code,
            "Skema Uji",
            null,
            [
                new ProfitSharingSchemeParticipantDefinition(
                    "PERUSAHAAN",
                    "Perusahaan",
                    ProfitSharingParticipantRole.Company,
                    true,
                    1),
                new ProfitSharingSchemeParticipantDefinition(
                    "MITRA",
                    "Mitra Tani",
                    ProfitSharingParticipantRole.ManagingPartner,
                    true,
                    2),
                new ProfitSharingSchemeParticipantDefinition(
                    "INVESTOR",
                    "Investor Pasif",
                    ProfitSharingParticipantRole.PassiveInvestor,
                    true,
                    3)
            ],
            [
                new ProfitSharingSchemePriorityRuleDefinition(
                    "BIAYA-KELOLA",
                    ProfitSharingPriorityRuleType.ManagementShare,
                    "MITRA",
                    ProfitSharingRate.FromFraction(1m, 3m),
                    1)
            ],
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            []);

        if (active)
        {
            scheme.Activate();
        }

        return scheme;
    }

    private sealed class FakeAssignmentRepository :
        IProfitSharingSchemeAssignmentRepository
    {
        public FakeAssignmentRepository(
            params ProfitSharingSchemeAssignment[] assignments)
        {
            Assignments.AddRange(assignments);
        }

        public List<ProfitSharingSchemeAssignment> Assignments
        {
            get;
        } = [];

        public Task<ProfitSharingSchemeAssignment?>
            GetByCropCycleAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(
                organizationId,
                cropCycleId));
        }

        public Task<ProfitSharingSchemeAssignment?>
            GetByCropCycleForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(
                organizationId,
                cropCycleId));
        }

        public void Add(ProfitSharingSchemeAssignment assignment)
        {
            Assignments.Add(assignment);
        }

        private ProfitSharingSchemeAssignment? Find(
            Guid organizationId,
            Guid cropCycleId)
        {
            return Assignments.SingleOrDefault(assignment =>
                assignment.OrganizationId == organizationId &&
                assignment.CropCycleId == cropCycleId &&
                !assignment.IsDeleted);
        }
    }

    private sealed class FakeSchemeRepository :
        IProfitSharingSchemeRepository
    {
        private readonly List<ProfitSharingScheme> _schemes = [];

        public FakeSchemeRepository(
            params ProfitSharingScheme[] schemes)
        {
            _schemes.AddRange(schemes);
        }

        public Task<IReadOnlyList<ProfitSharingScheme>> GetAllAsync(
            Guid organizationId,
            ProfitSharingSchemeStatus? status = null,
            string? code = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProfitSharingScheme> result = _schemes
                .Where(scheme =>
                    scheme.OrganizationId == organizationId &&
                    (!status.HasValue ||
                     scheme.Status == status.Value) &&
                    (code is null || scheme.Code == code))
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<ProfitSharingScheme?> GetByIdAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(organizationId, schemeId));
        }

        public Task<ProfitSharingScheme?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(organizationId, schemeId));
        }

        public Task<ProfitSharingScheme?> GetActiveForUpdateAsync(
            Guid organizationId,
            Guid schemeFamilyId,
            Guid? excludedSchemeId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_schemes.SingleOrDefault(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.SchemeFamilyId == schemeFamilyId &&
                scheme.Status == ProfitSharingSchemeStatus.Active &&
                (!excludedSchemeId.HasValue ||
                 scheme.Id != excludedSchemeId.Value)));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_schemes.Any(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.Code == code));
        }

        public Task<bool> HasDraftAsync(
            Guid organizationId,
            Guid schemeFamilyId,
            Guid? excludedSchemeId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_schemes.Any(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.SchemeFamilyId == schemeFamilyId &&
                scheme.Status == ProfitSharingSchemeStatus.Draft &&
                (!excludedSchemeId.HasValue ||
                 scheme.Id != excludedSchemeId.Value)));
        }

        public void Add(ProfitSharingScheme scheme)
        {
            _schemes.Add(scheme);
        }

        private ProfitSharingScheme? Find(
            Guid organizationId,
            Guid schemeId)
        {
            return _schemes.SingleOrDefault(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.Id == schemeId &&
                !scheme.IsDeleted);
        }
    }

    private sealed class FakeCropCycleRepository :
        ICropCycleRepository
    {
        private readonly CropCycle? _cropCycle;

        public FakeCropCycleRepository(CropCycle? cropCycle = null)
        {
            _cropCycle = cropCycle;
        }

        public Task<IReadOnlyList<CropCycle>> GetAllAsync(
            Guid organizationId,
            CropCycleStatus? status = null,
            Guid? commodityId = null,
            Guid? landId = null,
            Guid? landPlotId = null,
            DateOnly? plannedStartFrom = null,
            DateOnly? plannedStartTo = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CropCycle> result =
                _cropCycle?.OrganizationId == organizationId
                    ? [_cropCycle]
                    : [];

            return Task.FromResult(result);
        }

        public Task<CropCycle?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(
                organizationId,
                cropCycleId));
        }

        public Task<CropCycle?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Find(
                organizationId,
                cropCycleId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cropCycle is not null &&
                _cropCycle.OrganizationId == organizationId &&
                _cropCycle.Code == code);
        }

        public Task<bool> HasScheduleConflictAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            DateOnly plannedStartDate,
            DateOnly expectedHarvestDate,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasInProgressCycleAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasActiveCycleForLandAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasActiveCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasAnyCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(CropCycle cropCycle)
        {
            throw new NotSupportedException();
        }

        private CropCycle? Find(
            Guid organizationId,
            Guid cropCycleId)
        {
            return _cropCycle?.OrganizationId == organizationId &&
                   _cropCycle.Id == cropCycleId &&
                   !_cropCycle.IsDeleted
                ? _cropCycle
                : null;
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
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _organization?.Id == organizationId
                    ? _organization
                    : null);
        }

        public Task<Organization?> GetByIdForUpdateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(organizationId, cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_organization?.Code == code);
        }

        public void Add(Organization organization)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
