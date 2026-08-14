using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed class ProfitSharingSchemeTests
{
    [Fact]
    public void CreateDraft_WithWaterfallDefinition_ShouldNormalize()
    {
        var organizationId = Guid.NewGuid();

        var scheme = CreateDraft(organizationId);

        Assert.NotEqual(Guid.Empty, scheme.Id);
        Assert.Equal(scheme.Id, scheme.SchemeFamilyId);
        Assert.Equal(organizationId, scheme.OrganizationId);
        Assert.Equal("BAGI-HASIL-UTAMA", scheme.Code);
        Assert.Equal("Skema utama", scheme.Name);
        Assert.Equal("Versi awal", scheme.Description);
        Assert.Equal(1, scheme.Version);
        Assert.Equal(
            ProfitSharingSchemeStatus.Draft,
            scheme.Status);
        Assert.Equal(3, scheme.Participants.Count);
        Assert.Equal(2, scheme.PriorityRules.Count);
        Assert.Empty(scheme.ResidualShares);
        Assert.Equal(
            ProfitSharingResidualMethod.ProRataCapital,
            scheme.ResidualMethod);
    }

    [Fact]
    public void CreateDraft_WithDuplicateParticipant_ShouldReject()
    {
        var participants = Participants().ToArray();

        participants[1] = participants[1] with
        {
            ParticipantCode = "PERUSAHAAN"
        };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingScheme.CreateDraft(
                Guid.NewGuid(),
                "SCHEME",
                "Scheme",
                null,
                participants,
                PriorityRules(),
                ProfitSharingResidualMethod.ProRataCapital,
                null,
                []));
    }

    [Fact]
    public void CreateDraft_WithUnknownRuleRecipient_ShouldReject()
    {
        var rules = PriorityRules().ToArray();

        rules[0] = rules[0] with
        {
            RecipientCode = "TIDAK-ADA"
        };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingScheme.CreateDraft(
                Guid.NewGuid(),
                "SCHEME",
                "Scheme",
                null,
                Participants(),
                rules,
                ProfitSharingResidualMethod.ProRataCapital,
                null,
                []));
    }

    [Fact]
    public void CreateDraft_WithInvalidFixedTotal_ShouldReject()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingScheme.CreateDraft(
                Guid.NewGuid(),
                "SCHEME",
                "Scheme",
                null,
                Participants(),
                PriorityRules(),
                ProfitSharingResidualMethod.FixedPercentage,
                null,
                [
                    new ProfitSharingSchemeResidualShareDefinition(
                        "PERUSAHAAN",
                        ProfitSharingRate.FromPercentage(70),
                        1),
                    new ProfitSharingSchemeResidualShareDefinition(
                        "MITRA",
                        ProfitSharingRate.FromPercentage(20),
                        2)
                ]));
    }

    [Fact]
    public void UpdateDraft_ShouldReplaceDefinition()
    {
        var scheme = CreateDraft();

        scheme.UpdateDraft(
            "Skema revisi",
            null,
            Participants(),
            [
                new ProfitSharingSchemePriorityRuleDefinition(
                    "KELOLA-MITRA",
                    ProfitSharingPriorityRuleType.ManagementShare,
                    "MITRA",
                    ProfitSharingRate.FromPercentage(25),
                    1)
            ],
            ProfitSharingResidualMethod
                .RemainderToParticipant,
            "PERUSAHAAN",
            []);

        Assert.Equal("Skema revisi", scheme.Name);
        Assert.Null(scheme.Description);
        Assert.Single(scheme.PriorityRules);
        Assert.Equal(
            ProfitSharingResidualMethod
                .RemainderToParticipant,
            scheme.ResidualMethod);
        Assert.Equal(
            "PERUSAHAAN",
            scheme.ResidualRecipientCode);
        Assert.NotNull(scheme.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldMakeDefinitionImmutable()
    {
        var scheme = CreateDraft();

        scheme.Activate();

        Assert.Equal(
            ProfitSharingSchemeStatus.Active,
            scheme.Status);
        Assert.NotNull(scheme.ActivatedAt);

        Assert.Throws<InvalidOperationException>(() =>
            scheme.UpdateDraft(
                "Tidak boleh",
                null,
                Participants(),
                PriorityRules(),
                ProfitSharingResidualMethod.ProRataCapital,
                null,
                []));
    }

    [Fact]
    public void CreateNextVersion_ShouldCloneActiveSnapshot()
    {
        var source = CreateDraft();
        source.Activate();

        var next =
            ProfitSharingScheme.CreateNextVersion(source);

        Assert.NotEqual(source.Id, next.Id);
        Assert.Equal(
            source.SchemeFamilyId,
            next.SchemeFamilyId);
        Assert.Equal(source.Code, next.Code);
        Assert.Equal(2, next.Version);
        Assert.Equal(
            ProfitSharingSchemeStatus.Draft,
            next.Status);
        Assert.Equal(
            source.Participants.Count,
            next.Participants.Count);
        Assert.DoesNotContain(
            next.Participants,
            participant => source.Participants.Any(sourceParticipant =>
                sourceParticipant.Id == participant.Id));
    }

    [Fact]
    public void CreateNextVersion_FromDraft_ShouldReject()
    {
        var source = CreateDraft();

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingScheme.CreateNextVersion(source));
    }

    [Fact]
    public void Supersede_ShouldCloseActiveVersion()
    {
        var scheme = CreateDraft();
        scheme.Activate();

        scheme.Supersede();

        Assert.Equal(
            ProfitSharingSchemeStatus.Superseded,
            scheme.Status);
        Assert.NotNull(scheme.SupersededAt);
    }

    [Fact]
    public void ProRataCapital_WithoutEligibleParticipant_ShouldReject()
    {
        var participants = Participants()
            .Select(participant => participant with
            {
                ParticipatesInResidualProfit = false
            })
            .ToArray();

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingScheme.CreateDraft(
                Guid.NewGuid(),
                "SCHEME",
                "Scheme",
                null,
                participants,
                PriorityRules(),
                ProfitSharingResidualMethod.ProRataCapital,
                null,
                []));
    }

    private static ProfitSharingScheme CreateDraft(
        Guid? organizationId = null)
    {
        return ProfitSharingScheme.CreateDraft(
            organizationId ?? Guid.NewGuid(),
            "  bagi-hasil-utama  ",
            "  Skema utama  ",
            "  Versi awal  ",
            Participants(),
            PriorityRules(),
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            []);
    }

    private static IReadOnlyCollection<
        ProfitSharingSchemeParticipantDefinition>
        Participants()
    {
        return
        [
            new ProfitSharingSchemeParticipantDefinition(
                "PERUSAHAAN",
                "Perusahaan",
                ProfitSharingParticipantRole.Company,
                true,
                1),
            new ProfitSharingSchemeParticipantDefinition(
                "INVESTOR-A",
                "Investor Pasif A",
                ProfitSharingParticipantRole.PassiveInvestor,
                false,
                2),
            new ProfitSharingSchemeParticipantDefinition(
                "MITRA",
                "Mitra Tani",
                ProfitSharingParticipantRole.ManagingPartner,
                true,
                3)
        ];
    }

    private static IReadOnlyCollection<
        ProfitSharingSchemePriorityRuleDefinition>
        PriorityRules()
    {
        return
        [
            new ProfitSharingSchemePriorityRuleDefinition(
                "KELOLA-MITRA",
                ProfitSharingPriorityRuleType.ManagementShare,
                "MITRA",
                ProfitSharingRate.FromFraction(1, 3),
                1),
            new ProfitSharingSchemePriorityRuleDefinition(
                "IMBAL-INVESTOR-A",
                ProfitSharingPriorityRuleType.ReturnOnCapital,
                "INVESTOR-A",
                ProfitSharingRate.FromPercentage(15),
                2)
        ];
    }
}
