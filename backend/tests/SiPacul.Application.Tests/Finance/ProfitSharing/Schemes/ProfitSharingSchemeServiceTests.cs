using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Schemes;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeServiceTests
{
    [Fact]
    public async Task CreateDraft_WithValidDefinition_ShouldSave()
    {
        var organization = CreateOrganization();
        var repository = new FakeSchemeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            repository,
            unitOfWork);

        var result = await service.CreateDraftAsync(
            organization.Id,
            CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("BAGI-HASIL-UTAMA", result.Value.Code);
        Assert.Equal(1, result.Value.Version);
        Assert.Equal(
            ProfitSharingSchemeStatus.Draft,
            result.Value.Status);
        Assert.Single(repository.Schemes);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDraft_WhenCodeExists_ShouldConflict()
    {
        var organization = CreateOrganization();
        var existing = CreateScheme(organization.Id);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            new FakeSchemeRepository(existing),
            unitOfWork);

        var result = await service.CreateDraftAsync(
            organization.Id,
            CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeErrors.CodeAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDraft_WhenOrganizationMissing_ShouldNotFound()
    {
        var service = new ProfitSharingSchemeService(
            new FakeSchemeRepository(),
            new FakeOrganizationRepository(),
            new FakeActivationProcessor(
                new FakeSchemeRepository()),
            new FakeUnitOfWork());

        var result = await service.CreateDraftAsync(
            Guid.NewGuid(),
            CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Activate_FirstVersion_ShouldBecomeActive()
    {
        var organization = CreateOrganization();
        var scheme = CreateScheme(organization.Id);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            new FakeSchemeRepository(scheme),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id,
            scheme.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ProfitSharingSchemeStatus.Active,
            result.Value.Status);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateNextVersion_ShouldCloneActiveVersion()
    {
        var organization = CreateOrganization();
        var active = CreateScheme(organization.Id);
        active.Activate();
        var repository = new FakeSchemeRepository(active);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            repository,
            unitOfWork);

        var result = await service.CreateNextVersionAsync(
            organization.Id,
            active.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Version);
        Assert.Equal(
            active.SchemeFamilyId,
            result.Value.SchemeFamilyId);
        Assert.Equal(
            ProfitSharingSchemeStatus.Draft,
            result.Value.Status);
        Assert.Equal(2, repository.Schemes.Count);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateNextVersion_WhenDraftExists_ShouldConflict()
    {
        var organization = CreateOrganization();
        var active = CreateScheme(organization.Id);
        active.Activate();
        var existingDraft =
            ProfitSharingScheme.CreateNextVersion(active);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            new FakeSchemeRepository(active, existingDraft),
            unitOfWork);

        var result = await service.CreateNextVersionAsync(
            organization.Id,
            active.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeErrors.DraftAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_NextVersion_ShouldSupersedePrevious()
    {
        var organization = CreateOrganization();
        var active = CreateScheme(organization.Id);
        active.Activate();
        var draft = ProfitSharingScheme.CreateNextVersion(active);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            new FakeSchemeRepository(active, draft),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id,
            draft.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ProfitSharingSchemeStatus.Active,
            draft.Status);
        Assert.Equal(
            ProfitSharingSchemeStatus.Superseded,
            active.Status);
        Assert.NotNull(active.SupersededAt);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenActive_ShouldConflict()
    {
        var organization = CreateOrganization();
        var active = CreateScheme(organization.Id);
        active.Activate();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            organization,
            new FakeSchemeRepository(active),
            unitOfWork);

        var result = await service.UpdateDraftAsync(
            organization.Id,
            active.Id,
            UpdateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ProfitSharingSchemeErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetAll_ShouldFilterStatusAndCode()
    {
        var organization = CreateOrganization();
        var active = CreateScheme(organization.Id);
        active.Activate();
        var draft = ProfitSharingScheme.CreateNextVersion(active);
        var service = CreateService(
            organization,
            new FakeSchemeRepository(active, draft),
            new FakeUnitOfWork());

        var result = await service.GetAllAsync(
            organization.Id,
            new ProfitSharingSchemeFilter(
                ProfitSharingSchemeStatus.Active,
                "bagi-hasil-utama"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(active.Id, result.Value[0].Id);
    }

    private static ProfitSharingSchemeService CreateService(
        Organization organization,
        FakeSchemeRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        return new ProfitSharingSchemeService(
            repository,
            new FakeOrganizationRepository(organization),
            new FakeActivationProcessor(repository),
            unitOfWork);
    }

    private static Organization CreateOrganization()
    {
        return Organization.Create(
            "ORG-001",
            "Organisasi Uji");
    }

    private static ProfitSharingScheme CreateScheme(
        Guid organizationId)
    {
        var request = CreateRequest();

        return ProfitSharingScheme.CreateDraft(
            organizationId,
            request.Code,
            request.Name,
            request.Description,
            request.Participants
                .Select(participant =>
                    new ProfitSharingSchemeParticipantDefinition(
                        participant.ParticipantCode,
                        participant.ParticipantName,
                        participant.ParticipantRole,
                        participant.ParticipatesInResidualProfit,
                        participant.Sequence))
                .ToArray(),
            request.PriorityRules
                .Select(rule =>
                    new ProfitSharingSchemePriorityRuleDefinition(
                        rule.RuleCode,
                        rule.RuleType,
                        rule.RecipientCode,
                        ProfitSharingRate.FromFraction(
                            rule.RateNumerator,
                            rule.RateDenominator),
                        rule.Sequence))
                .ToArray(),
            request.ResidualMethod,
            request.ResidualRecipientCode,
            []);
    }

    private static CreateProfitSharingSchemeRequest CreateRequest()
    {
        return new CreateProfitSharingSchemeRequest(
            "bagi-hasil-utama",
            "Skema Utama",
            null,
            [
                new ProfitSharingSchemeParticipantRequest(
                    "PERUSAHAAN",
                    "Perusahaan",
                    ProfitSharingParticipantRole.Company,
                    true,
                    1),
                new ProfitSharingSchemeParticipantRequest(
                    "MITRA",
                    "Mitra Tani",
                    ProfitSharingParticipantRole.ManagingPartner,
                    true,
                    2)
            ],
            [
                new ProfitSharingSchemePriorityRuleRequest(
                    "KELOLA-MITRA",
                    ProfitSharingPriorityRuleType.ManagementShare,
                    "MITRA",
                    1m,
                    3m,
                    1)
            ],
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            []);
    }

    private static UpdateProfitSharingSchemeDraftRequest
        UpdateRequest()
    {
        var create = CreateRequest();

        return new UpdateProfitSharingSchemeDraftRequest(
            "Skema Revisi",
            null,
            create.Participants,
            create.PriorityRules,
            create.ResidualMethod,
            create.ResidualRecipientCode,
            create.ResidualShares);
    }

    private sealed class FakeSchemeRepository :
        IProfitSharingSchemeRepository
    {
        public FakeSchemeRepository(
            params ProfitSharingScheme[] schemes)
        {
            Schemes.AddRange(schemes);
        }

        public List<ProfitSharingScheme> Schemes { get; } = [];

        public Task<IReadOnlyList<ProfitSharingScheme>> GetAllAsync(
            Guid organizationId,
            ProfitSharingSchemeStatus? status = null,
            string? code = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProfitSharingScheme> result = Schemes
                .Where(scheme =>
                    scheme.OrganizationId == organizationId &&
                    !scheme.IsDeleted &&
                    (!status.HasValue ||
                     scheme.Status == status.Value) &&
                    (code is null || scheme.Code == code))
                .OrderBy(scheme => scheme.Code)
                .ThenByDescending(scheme => scheme.Version)
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
            var scheme = Schemes.SingleOrDefault(candidate =>
                candidate.OrganizationId == organizationId &&
                candidate.SchemeFamilyId == schemeFamilyId &&
                candidate.Status == ProfitSharingSchemeStatus.Active &&
                (!excludedSchemeId.HasValue ||
                 candidate.Id != excludedSchemeId.Value));

            return Task.FromResult(scheme);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Schemes.Any(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.Code == code &&
                !scheme.IsDeleted));
        }

        public Task<bool> HasDraftAsync(
            Guid organizationId,
            Guid schemeFamilyId,
            Guid? excludedSchemeId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Schemes.Any(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.SchemeFamilyId == schemeFamilyId &&
                scheme.Status == ProfitSharingSchemeStatus.Draft &&
                (!excludedSchemeId.HasValue ||
                 scheme.Id != excludedSchemeId.Value)));
        }

        public void Add(ProfitSharingScheme scheme)
        {
            Schemes.Add(scheme);
        }

        private ProfitSharingScheme? Find(
            Guid organizationId,
            Guid schemeId)
        {
            return Schemes.SingleOrDefault(scheme =>
                scheme.OrganizationId == organizationId &&
                scheme.Id == schemeId &&
                !scheme.IsDeleted);
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
            IReadOnlyList<Organization> organizations =
                _organization is null
                    ? []
                    : [_organization];

            return Task.FromResult(organizations);
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
            return Task.FromResult(
                _organization?.Code == code);
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

    private sealed class FakeActivationProcessor :
        IProfitSharingSchemeActivationProcessor
    {
        private readonly FakeSchemeRepository _repository;

        public FakeActivationProcessor(
            FakeSchemeRepository repository)
        {
            _repository = repository;
        }

        public Task<ProfitSharingSchemeActivationResult>
            ActivateAsync(
                Guid organizationId,
                Guid schemeId,
                CancellationToken cancellationToken = default)
        {
            var scheme = _repository.Schemes.SingleOrDefault(
                candidate =>
                    candidate.OrganizationId == organizationId &&
                    candidate.Id == schemeId &&
                    !candidate.IsDeleted);

            if (scheme is null)
            {
                return Task.FromResult(
                    ProfitSharingSchemeActivationResult.Failed(
                        ProfitSharingSchemeActivationFailure
                            .SchemeNotFound));
            }

            if (scheme.Status != ProfitSharingSchemeStatus.Draft)
            {
                return Task.FromResult(
                    ProfitSharingSchemeActivationResult.Failed(
                        ProfitSharingSchemeActivationFailure
                            .InvalidStatus,
                        "Only a draft scheme can be activated."));
            }

            var active = _repository.Schemes.SingleOrDefault(
                candidate =>
                    candidate.OrganizationId == organizationId &&
                    candidate.SchemeFamilyId ==
                        scheme.SchemeFamilyId &&
                    candidate.Id != scheme.Id &&
                    candidate.Status ==
                        ProfitSharingSchemeStatus.Active);

            active?.Supersede();
            scheme.Activate();

            return Task.FromResult(
                ProfitSharingSchemeActivationResult.Succeeded(
                    scheme));
        }
    }
}
