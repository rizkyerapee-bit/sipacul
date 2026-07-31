using SiPacul.Domain.Entities.Cultivation;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Cultivation;

public sealed class CultivationActivityTests
{
    private static readonly DateOnly PlannedDate =
        new(2027, 1, 5);

    private static readonly DateOnly ActualStartDate =
        new(2027, 1, 6);

    private static readonly DateOnly CompletionDate =
        new(2027, 1, 8);

    [Fact]
    public void Create_WithoutSop_ShouldCreatePlannedActivity()
    {
        var activity = CreateActivity();

        Assert.NotEqual(Guid.Empty, activity.Id);
        Assert.Equal(
            CultivationActivityStatus.Planned,
            activity.Status);
        Assert.Equal(
            SopComplianceStatus.NotApplicable,
            activity.SopComplianceStatus);
        Assert.False(activity.IsLinkedToSopStep);
        Assert.Empty(activity.Resources);
        Assert.Equal(0m, activity.TotalActualCost);
    }

    [Fact]
    public void Create_ShouldNormalizeCodeNameAndNotes()
    {
        var activity = CreateActivity(
            code: "  olah-lahan_01  ",
            name: "  Pengolahan Lahan  ",
            notes: "  Gunakan traktor  ");

        Assert.Equal(
            "OLAH-LAHAN_01",
            activity.Code);
        Assert.Equal(
            "Pengolahan Lahan",
            activity.Name);
        Assert.Equal(
            "Gunakan traktor",
            activity.Notes);
    }

    [Fact]
    public void Create_WithSopSnapshot_ShouldPreserveSnapshot()
    {
        var sopId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        var activity = CreateLinkedActivity(
            sopId,
            stepId);

        Assert.True(activity.IsLinkedToSopStep);
        Assert.Equal(sopId, activity.CultivationSopId);
        Assert.Equal(
            stepId,
            activity.CultivationSopStepId);
        Assert.Equal(
            2,
            activity.SopStepSequenceSnapshot);
        Assert.Equal(
            "Pengolahan Tanah",
            activity.SopStepNameSnapshot);
        Assert.Equal(
            -14,
            activity.SopPlannedDayOffsetSnapshot);
        Assert.Equal(
            3,
            activity.SopEstimatedDurationDaysSnapshot);
        Assert.True(
            activity.SopIsRequiredSnapshot);
        Assert.Equal(
            SopComplianceStatus.NotEvaluated,
            activity.SopComplianceStatus);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CultivationActivity.Create(
                Guid.Empty,
                Guid.NewGuid(),
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                PlannedDate,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
    }

    [Fact]
    public void Create_WithEmptyCropCycleId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CultivationActivity.Create(
                Guid.NewGuid(),
                Guid.Empty,
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                PlannedDate,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ACT 001")]
    [InlineData("ACT#001")]
    public void Create_WithInvalidCode_ShouldThrow(
        string code)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateActivity(code: code));
    }

    [Fact]
    public void Create_WithUnsupportedActivityType_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateActivity(
                activityType:
                    (CultivationActivityType)999));
    }

    [Fact]
    public void Create_WithDefaultPlannedDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CultivationActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                default,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
    }

    [Fact]
    public void Create_WithIncompleteSopSnapshot_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CultivationActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                PlannedDate,
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null,
                null,
                null));
    }

    [Fact]
    public void Create_WithEmptySopIdentifier_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateLinkedActivity(
                Guid.Empty,
                Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithInvalidSnapshotSequence_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CultivationActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                PlannedDate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                0,
                "Pengolahan Tanah",
                -14,
                3,
                true,
                null));
    }

    [Fact]
    public void Create_WithInvalidSnapshotDuration_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CultivationActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ACT-001",
                "Pengolahan Lahan",
                CultivationActivityType.LandPreparation,
                PlannedDate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                "Pengolahan Tanah",
                -14,
                0,
                true,
                null));
    }

    [Fact]
    public void UpdatePlan_WhilePlanned_ShouldUpdate()
    {
        var activity = CreateActivity();

        activity.UpdatePlan(
            "  Persiapan Bedengan  ",
            CultivationActivityType.LandPreparation,
            PlannedDate.AddDays(2),
            "  Setelah hujan  ");

        Assert.Equal(
            "Persiapan Bedengan",
            activity.Name);
        Assert.Equal(
            PlannedDate.AddDays(2),
            activity.PlannedDate);
        Assert.Equal(
            "Setelah hujan",
            activity.Notes);
        Assert.NotNull(activity.UpdatedAt);
    }

    [Fact]
    public void UpdatePlan_WithSameValues_ShouldNotUpdateTimestamp()
    {
        var activity = CreateActivity();

        activity.UpdatePlan(
            "  Pengolahan Lahan  ",
            CultivationActivityType.LandPreparation,
            PlannedDate,
            "   ");

        Assert.Null(activity.UpdatedAt);
    }

    [Fact]
    public void UpdatePlan_AfterStart_ShouldThrow()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<InvalidOperationException>(() =>
            activity.UpdatePlan(
                "Rencana Baru",
                CultivationActivityType.Other,
                PlannedDate,
                null));
    }

    [Fact]
    public void Start_FromPlanned_ShouldSetActualDateAndStatus()
    {
        var activity = CreateActivity();

        activity.Start(ActualStartDate);

        Assert.Equal(
            CultivationActivityStatus.InProgress,
            activity.Status);
        Assert.Equal(
            ActualStartDate,
            activity.ActualStartDate);
        Assert.NotNull(activity.UpdatedAt);
    }

    [Fact]
    public void Start_WithDefaultDate_ShouldThrowWithoutMutation()
    {
        var activity = CreateActivity();

        Assert.Throws<ArgumentException>(() =>
            activity.Start(default));

        Assert.Equal(
            CultivationActivityStatus.Planned,
            activity.Status);
        Assert.Null(activity.ActualStartDate);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_ShouldThrow()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<InvalidOperationException>(() =>
            activity.Start(ActualStartDate));
    }

    [Fact]
    public void Complete_UnlinkedActivity_ShouldUseNotApplicable()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        activity.Complete(
            CompletionDate,
            "  Lahan siap ditanami  ",
            "  Hujan ringan  ",
            SopComplianceStatus.NotApplicable,
            null);

        Assert.Equal(
            CultivationActivityStatus.Completed,
            activity.Status);
        Assert.Equal(
            CompletionDate,
            activity.ActualCompletionDate);
        Assert.Equal(
            "Lahan siap ditanami",
            activity.Outcome);
        Assert.Equal(
            "Hujan ringan",
            activity.IssueNotes);
        Assert.Equal(
            SopComplianceStatus.NotApplicable,
            activity.SopComplianceStatus);
    }

    [Fact]
    public void Complete_LinkedCompliantActivity_ShouldComplete()
    {
        var activity = CreateLinkedActivity();
        activity.Start(ActualStartDate);

        activity.Complete(
            CompletionDate,
            "Pekerjaan selesai",
            null,
            SopComplianceStatus.Compliant,
            null);

        Assert.Equal(
            CultivationActivityStatus.Completed,
            activity.Status);
        Assert.Equal(
            SopComplianceStatus.Compliant,
            activity.SopComplianceStatus);
        Assert.Null(activity.DeviationReason);
    }

    [Fact]
    public void Complete_LinkedDeviatedActivity_ShouldStoreReason()
    {
        var activity = CreateLinkedActivity();
        activity.Start(ActualStartDate);

        activity.Complete(
            CompletionDate,
            null,
            "Traktor terlambat",
            SopComplianceStatus.Deviated,
            "  Pelaksanaan mundur dua hari  ");

        Assert.Equal(
            SopComplianceStatus.Deviated,
            activity.SopComplianceStatus);
        Assert.Equal(
            "Pelaksanaan mundur dua hari",
            activity.DeviationReason);
    }

    [Fact]
    public void Complete_LinkedNotEvaluatedActivity_ShouldThrow()
    {
        var activity = CreateLinkedActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<ArgumentException>(() =>
            activity.Complete(
                CompletionDate,
                null,
                null,
                SopComplianceStatus.NotEvaluated,
                null));

        Assert.Equal(
            CultivationActivityStatus.InProgress,
            activity.Status);
    }

    [Fact]
    public void Complete_DeviatedWithoutReason_ShouldThrow()
    {
        var activity = CreateLinkedActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<ArgumentException>(() =>
            activity.Complete(
                CompletionDate,
                null,
                null,
                SopComplianceStatus.Deviated,
                "  "));
    }

    [Fact]
    public void Complete_UnlinkedWithCompliance_ShouldThrow()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<ArgumentException>(() =>
            activity.Complete(
                CompletionDate,
                null,
                null,
                SopComplianceStatus.Compliant,
                null));
    }

    [Fact]
    public void Complete_BeforeStartDate_ShouldThrow()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            activity.Complete(
                ActualStartDate.AddDays(-1),
                null,
                null,
                SopComplianceStatus.NotApplicable,
                null));
    }

    [Fact]
    public void Complete_FromPlanned_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<InvalidOperationException>(() =>
            activity.Complete(
                CompletionDate,
                null,
                null,
                SopComplianceStatus.NotApplicable,
                null));
    }

    [Fact]
    public void Cancel_FromPlanned_ShouldStoreReason()
    {
        var activity = CreateActivity();

        activity.Cancel(
            "  Cuaca tidak memungkinkan  ");

        Assert.Equal(
            CultivationActivityStatus.Cancelled,
            activity.Status);
        Assert.Equal(
            "Cuaca tidak memungkinkan",
            activity.CancellationReason);
    }

    [Fact]
    public void Cancel_FromInProgress_ShouldBeAllowed()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        activity.Cancel("Alat rusak");

        Assert.Equal(
            CultivationActivityStatus.Cancelled,
            activity.Status);
    }

    [Fact]
    public void Cancel_WithBlankReason_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<ArgumentException>(() =>
            activity.Cancel("  "));

        Assert.Equal(
            CultivationActivityStatus.Planned,
            activity.Status);
    }

    [Fact]
    public void Cancel_FromCompleted_ShouldThrow()
    {
        var activity = CompleteUnlinkedActivity();

        Assert.Throws<InvalidOperationException>(() =>
            activity.Cancel("Tidak berlaku"));
    }

    [Fact]
    public void UpdateExecutionNotes_WhileInProgress_ShouldUpdate()
    {
        var activity = CreateActivity();
        activity.Start(ActualStartDate);

        activity.UpdateExecutionNotes(
            "  Catatan umum  ",
            "  Pompa sempat macet  ");

        Assert.Equal(
            "Catatan umum",
            activity.Notes);
        Assert.Equal(
            "Pompa sempat macet",
            activity.IssueNotes);
    }

    [Fact]
    public void UpdateExecutionNotes_WithSameValues_ShouldNotUpdateTimestamp()
    {
        var activity = CreateActivity(
            notes: "Catatan");

        activity.UpdateExecutionNotes(
            "  Catatan  ",
            null);

        Assert.Null(activity.UpdatedAt);
    }

    [Fact]
    public void UpdateExecutionNotes_AfterCompletion_ShouldThrow()
    {
        var activity = CompleteUnlinkedActivity();

        Assert.Throws<InvalidOperationException>(() =>
            activity.UpdateExecutionNotes(
                "Tidak boleh",
                null));
    }

    [Theory]
    [InlineData(CultivationResourceType.Material)]
    [InlineData(CultivationResourceType.Labor)]
    [InlineData(CultivationResourceType.Equipment)]
    [InlineData(CultivationResourceType.Service)]
    [InlineData(CultivationResourceType.Other)]
    public void AddResource_WithSupportedType_ShouldAdd(
        CultivationResourceType resourceType)
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            resourceType,
            "  Sumber daya uji  ",
            2,
            "  unit  ",
            1000,
            "  Catatan  ");

        Assert.Single(activity.Resources);
        Assert.Equal(
            resourceType,
            resource.ResourceType);
        Assert.Equal(
            "Sumber daya uji",
            resource.Description);
        Assert.Equal("unit", resource.Unit);
        Assert.Equal("Catatan", resource.Notes);
        Assert.Equal(2000m, resource.TotalCost);
    }

    [Fact]
    public void AddResource_ShouldRoundQuantityUnitCostAndTotal()
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            1.23456m,
            "kg",
            10.125m,
            null);

        Assert.Equal(1.2346m, resource.Quantity);
        Assert.Equal(10.13m, resource.UnitCost);
        Assert.Equal(12.51m, resource.TotalCost);
    }

    [Fact]
    public void AddResource_WithZeroQuantity_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            activity.AddResource(
                CultivationResourceType.Material,
                "Pupuk",
                0,
                "kg",
                1000,
                null));

        Assert.Empty(activity.Resources);
    }

    [Fact]
    public void AddResource_WithNegativeUnitCost_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            activity.AddResource(
                CultivationResourceType.Material,
                "Pupuk",
                1,
                "kg",
                -1,
                null));
    }

    [Fact]
    public void AddResource_WithUnsupportedType_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            activity.AddResource(
                (CultivationResourceType)999,
                "Pupuk",
                1,
                "kg",
                1000,
                null));
    }

    [Fact]
    public void TotalActualCost_ShouldSumAllResourceLines()
    {
        var activity = CreateActivity();

        activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            10,
            "kg",
            4500,
            null);

        activity.AddResource(
            CultivationResourceType.Labor,
            "Tenaga kerja",
            2,
            "orang-hari",
            120000,
            null);

        Assert.Equal(
            285000m,
            activity.TotalActualCost);
    }

    [Fact]
    public void UpdateResource_ShouldUpdateValuesAndTotal()
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            CultivationResourceType.Equipment,
            "Traktor",
            2,
            "jam",
            200000,
            null);

        activity.UpdateResource(
            resource.Id,
            "  Traktor roda empat  ",
            3.5m,
            "  jam  ",
            210000,
            "  Termasuk operator  ");

        Assert.Equal(
            "Traktor roda empat",
            resource.Description);
        Assert.Equal(3.5m, resource.Quantity);
        Assert.Equal(210000m, resource.UnitCost);
        Assert.Equal(735000m, resource.TotalCost);
        Assert.Equal(
            "Termasuk operator",
            resource.Notes);
        Assert.NotNull(resource.UpdatedAt);
    }

    [Fact]
    public void UpdateResource_WithSameValues_ShouldNotChangeResourceTimestamp()
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            10,
            "kg",
            4500,
            null);

        activity.UpdateResource(
            resource.Id,
            "  Pupuk  ",
            10,
            "  kg  ",
            4500,
            "   ");

        Assert.Null(resource.UpdatedAt);
    }

    [Fact]
    public void UpdateResource_WhenMissing_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<KeyNotFoundException>(() =>
            activity.UpdateResource(
                Guid.NewGuid(),
                "Pupuk",
                1,
                "kg",
                1000,
                null));
    }

    [Fact]
    public void RemoveResource_WhenFound_ShouldRemove()
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            1,
            "kg",
            1000,
            null);

        activity.RemoveResource(resource.Id);

        Assert.Empty(activity.Resources);
        Assert.Equal(0m, activity.TotalActualCost);
    }

    [Fact]
    public void RemoveResource_WhenMissing_ShouldThrow()
    {
        var activity = CreateActivity();

        Assert.Throws<KeyNotFoundException>(() =>
            activity.RemoveResource(Guid.NewGuid()));
    }

    [Fact]
    public void Resources_AfterCompletion_ShouldBeImmutable()
    {
        var activity = CreateActivity();

        var resource = activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            1,
            "kg",
            1000,
            null);

        activity.Start(ActualStartDate);

        activity.Complete(
            CompletionDate,
            null,
            null,
            SopComplianceStatus.NotApplicable,
            null);

        Assert.Throws<InvalidOperationException>(() =>
            activity.AddResource(
                CultivationResourceType.Labor,
                "Pekerja",
                1,
                "orang-hari",
                100000,
                null));

        Assert.Throws<InvalidOperationException>(() =>
            activity.UpdateResource(
                resource.Id,
                "Pupuk baru",
                2,
                "kg",
                1000,
                null));

        Assert.Throws<InvalidOperationException>(() =>
            activity.RemoveResource(resource.Id));

        Assert.Single(activity.Resources);
    }

    [Fact]
    public void Resources_AfterCancellation_ShouldBeImmutable()
    {
        var activity = CreateActivity();

        activity.AddResource(
            CultivationResourceType.Material,
            "Pupuk",
            1,
            "kg",
            1000,
            null);

        activity.Cancel("Rencana dibatalkan");

        Assert.Throws<InvalidOperationException>(() =>
            activity.RemoveResource(
                activity.Resources.Single().Id));
    }

    private static CultivationActivity CreateActivity(
        string code = "ACT-001",
        string name = "Pengolahan Lahan",
        CultivationActivityType activityType =
            CultivationActivityType.LandPreparation,
        DateOnly? plannedDate = null,
        string? notes = null)
    {
        return CultivationActivity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            code,
            name,
            activityType,
            plannedDate ?? PlannedDate,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            notes);
    }

    private static CultivationActivity CreateLinkedActivity(
        Guid? sopId = null,
        Guid? stepId = null)
    {
        return CultivationActivity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ACT-SOP-001",
            "Pengolahan Lahan Sesuai SOP",
            CultivationActivityType.LandPreparation,
            PlannedDate,
            sopId ?? Guid.NewGuid(),
            stepId ?? Guid.NewGuid(),
            2,
            "Pengolahan Tanah",
            -14,
            3,
            true,
            null);
    }

    private static CultivationActivity
        CompleteUnlinkedActivity()
    {
        var activity = CreateActivity();

        activity.Start(ActualStartDate);

        activity.Complete(
            CompletionDate,
            null,
            null,
            SopComplianceStatus.NotApplicable,
            null);

        return activity;
    }
}
