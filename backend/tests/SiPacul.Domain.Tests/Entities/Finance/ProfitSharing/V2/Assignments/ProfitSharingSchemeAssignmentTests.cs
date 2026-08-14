using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing.V2.Assignments;

public sealed class ProfitSharingSchemeAssignmentTests
{
    [Fact]
    public void Create_WithActiveScheme_ShouldCopyCompleteSnapshot()
    {
        var organizationId = Guid.NewGuid();
        var cropCycleId = Guid.NewGuid();
        var scheme = CreateActiveScheme(organizationId);

        var assignment = ProfitSharingSchemeAssignment.Create(
            organizationId,
            cropCycleId,
            scheme);

        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(organizationId, assignment.OrganizationId);
        Assert.Equal(cropCycleId, assignment.CropCycleId);
        Assert.Equal(scheme.Id, assignment.SourceSchemeId);
        Assert.Equal(scheme.SchemeFamilyId, assignment.SchemeFamilyId);
        Assert.Equal("SKEMA-UTAMA", assignment.SchemeCode);
        Assert.Equal("Skema Utama", assignment.SchemeName);
        Assert.Equal(1, assignment.SchemeVersion);
        Assert.Equal(3, assignment.Participants.Count);
        Assert.Equal(2, assignment.PriorityRules.Count);
        Assert.Empty(assignment.ResidualShares);
        Assert.Equal(
            ProfitSharingResidualMethod.ProRataCapital,
            assignment.ResidualMethod);
        Assert.NotEqual(default, assignment.AssignedAt);
    }

    [Fact]
    public void Create_ShouldUseIndependentChildIdentifiers()
    {
        var organizationId = Guid.NewGuid();
        var scheme = CreateActiveScheme(organizationId);

        var assignment = ProfitSharingSchemeAssignment.Create(
            organizationId,
            Guid.NewGuid(),
            scheme);

        Assert.DoesNotContain(
            assignment.Participants,
            snapshot => scheme.Participants.Any(source =>
                source.Id == snapshot.Id));
        Assert.DoesNotContain(
            assignment.PriorityRules,
            snapshot => scheme.PriorityRules.Any(source =>
                source.Id == snapshot.Id));
    }

    [Fact]
    public void Create_WithDraftScheme_ShouldReject()
    {
        var organizationId = Guid.NewGuid();
        var scheme = CreateDraftScheme(organizationId);

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingSchemeAssignment.Create(
                organizationId,
                Guid.NewGuid(),
                scheme));
    }

    [Fact]
    public void Create_WithDifferentOrganization_ShouldReject()
    {
        var scheme = CreateActiveScheme(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSchemeAssignment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                scheme));
    }

    [Fact]
    public void ReplaceSnapshot_ShouldReplaceDefinition()
    {
        var organizationId = Guid.NewGuid();
        var first = CreateActiveScheme(organizationId);
        var replacement = CreateDraftScheme(
            organizationId,
            "SKEMA-PENGGANTI",
            "Skema Pengganti");
        replacement.UpdateDraft(
            "Skema Pengganti",
            "Tanpa biaya pengelolaan",
            Participants(),
            [],
            ProfitSharingResidualMethod.RemainderToParticipant,
            "PERUSAHAAN",
            []);
        replacement.Activate();

        var assignment = ProfitSharingSchemeAssignment.Create(
            organizationId,
            Guid.NewGuid(),
            first);

        assignment.ReplaceSnapshot(replacement);

        Assert.Equal(replacement.Id, assignment.SourceSchemeId);
        Assert.Equal("SKEMA-PENGGANTI", assignment.SchemeCode);
        Assert.Equal(
            "Tanpa biaya pengelolaan",
            assignment.SchemeDescription);
        Assert.Empty(assignment.PriorityRules);
        Assert.Equal(
            ProfitSharingResidualMethod.RemainderToParticipant,
            assignment.ResidualMethod);
        Assert.Equal("PERUSAHAAN", assignment.ResidualRecipientCode);
        Assert.NotNull(assignment.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyCropCycleId_ShouldReject()
    {
        var organizationId = Guid.NewGuid();
        var scheme = CreateActiveScheme(organizationId);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSchemeAssignment.Create(
                organizationId,
                Guid.Empty,
                scheme));
    }

    private static ProfitSharingScheme CreateActiveScheme(
        Guid organizationId)
    {
        var scheme = CreateDraftScheme(organizationId);
        scheme.Activate();
        return scheme;
    }

    private static ProfitSharingScheme CreateDraftScheme(
        Guid organizationId,
        string code = "SKEMA-UTAMA",
        string name = "Skema Utama")
    {
        return ProfitSharingScheme.CreateDraft(
            organizationId,
            code,
            name,
            "Termasuk investor pasif",
            Participants(),
            [
                new ProfitSharingSchemePriorityRuleDefinition(
                    "BIAYA-KELOLA",
                    ProfitSharingPriorityRuleType.ManagementShare,
                    "MITRA",
                    ProfitSharingRate.FromFraction(1m, 3m),
                    1),
                new ProfitSharingSchemePriorityRuleDefinition(
                    "IMBAL-INVESTOR",
                    ProfitSharingPriorityRuleType.ReturnOnCapital,
                    "INVESTOR",
                    ProfitSharingRate.FromPercentage(10m),
                    2)
            ],
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            []);
    }

    private static IReadOnlyCollection<
        ProfitSharingSchemeParticipantDefinition> Participants()
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
        ];
    }
}
