using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

public sealed partial class ProfitSharingWaterfallSettlement :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxSchemeCodeLength = 40;

    public const int MaxSchemeNameLength = 150;

    public const int MaxSchemeDescriptionLength = 1000;

    public const int MaxCalculationVersionLength = 50;

    public const int MaxNotesLength = 1000;

    public const int MaxVoidReasonLength = 500;

    private readonly List<ProfitSharingWaterfallPriorityAllocation>
        _priorityAllocations = [];

    private readonly List<ProfitSharingWaterfallParticipantAllocation>
        _participantAllocations = [];

    private readonly List<
        ProfitSharingWaterfallResidualShareSnapshot>
        _residualShares = [];

    private ProfitSharingWaterfallSettlement()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid SourceSchemeId { get; private set; }

    public Guid SchemeFamilyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public DateOnly SettlementDate { get; private set; }

    public string SchemeCodeSnapshot { get; private set; } =
        string.Empty;

    public string SchemeNameSnapshot { get; private set; } =
        string.Empty;

    public string? SchemeDescriptionSnapshot { get; private set; }

    public int SchemeVersionSnapshot { get; private set; }

    public DateTime SchemeAssignedAtSnapshot { get; private set; }

    public ProfitSharingResidualMethod ResidualMethod
    {
        get;
        private set;
    }

    public string? ResidualRecipientCodeSnapshot { get; private set; }

    public string CropCycleCodeSnapshot { get; private set; } =
        string.Empty;

    public string CropCycleNameSnapshot { get; private set; } =
        string.Empty;

    public Guid CommodityIdSnapshot { get; private set; }

    public string CommodityCodeSnapshot { get; private set; } =
        string.Empty;

    public string CommodityNameSnapshot { get; private set; } =
        string.Empty;

    public decimal RecognizedRevenue { get; private set; }

    public decimal CollectedRevenue { get; private set; }

    public decimal OutstandingReceivable { get; private set; }

    public decimal ActivityResourceCost { get; private set; }

    public decimal ManualExpenseCost { get; private set; }

    public decimal TotalCultivationCost { get; private set; }

    public decimal NetProfit { get; private set; }

    public ProfitabilityOutcome Outcome { get; private set; }

    public decimal ConfirmedInvestorCapital { get; private set; }

    public decimal ConfirmedPartnerCapital { get; private set; }

    public decimal TotalConfirmedCapital { get; private set; }

    public decimal AvailableHarvestQuantity { get; private set; }

    public decimal TotalCapital { get; private set; }

    public decimal TotalCapitalRecovery { get; private set; }

    public decimal TotalCapitalLoss { get; private set; }

    public decimal TotalManagementProfitShare { get; private set; }

    public decimal TotalReturnOnCapitalProfitShare { get; private set; }

    public decimal TotalPriorityProfitShare { get; private set; }

    public decimal TotalResidualProfitShare { get; private set; }

    public decimal TotalProfitShare { get; private set; }

    public decimal TotalPayout { get; private set; }

    public string CalculationVersion { get; private set; } =
        string.Empty;

    public DateTime CalculatedAt { get; private set; }

    public string? Notes { get; private set; }

    public ProfitSharingWaterfallSettlementStatus Status
    {
        get;
        private set;
    }

    public DateTime FinalizedAt { get; private set; }

    public DateTime? VoidedAt { get; private set; }

    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<
        ProfitSharingWaterfallPriorityAllocation>
        PriorityAllocations => _priorityAllocations.AsReadOnly();

    public IReadOnlyCollection<
        ProfitSharingWaterfallParticipantAllocation>
        ParticipantAllocations => _participantAllocations.AsReadOnly();

    public IReadOnlyCollection<
        ProfitSharingWaterfallResidualShareSnapshot>
        ResidualShares => _residualShares.AsReadOnly();

    public bool IsActive =>
        Status == ProfitSharingWaterfallSettlementStatus.Finalized;

    public static ProfitSharingWaterfallSettlement CreateFinalized(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly settlementDate,
        ProfitSharingSchemeAssignment assignment,
        CropCycleProfitabilityReport profitability,
        ProfitSharingWaterfallCalculationResult calculation,
        string? notes,
        DateTime finalizedAt)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");
        ValidateIdentifier(
            cropCycleId,
            nameof(cropCycleId),
            "Crop cycle");
        ValidateSettlementDate(settlementDate);
        ValidateUtc(finalizedAt, nameof(finalizedAt));
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(profitability);
        ArgumentNullException.ThrowIfNull(calculation);

        ValidateSourceIdentity(
            organizationId,
            cropCycleId,
            assignment,
            profitability,
            calculation);
        ValidateFinalizationReadiness(profitability);
        ValidateCalculationMatchesReport(
            profitability,
            calculation);
        ValidateCalculationMatchesAssignment(
            assignment,
            calculation);

        var settlement = new ProfitSharingWaterfallSettlement
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            AssignmentId = assignment.Id,
            SourceSchemeId = assignment.SourceSchemeId,
            SchemeFamilyId = assignment.SchemeFamilyId,
            Code = NormalizeCode(code),
            SettlementDate = settlementDate,
            SchemeCodeSnapshot = assignment.SchemeCode,
            SchemeNameSnapshot = assignment.SchemeName,
            SchemeDescriptionSnapshot = assignment.SchemeDescription,
            SchemeVersionSnapshot = assignment.SchemeVersion,
            SchemeAssignedAtSnapshot = assignment.AssignedAt,
            ResidualMethod = assignment.ResidualMethod,
            ResidualRecipientCodeSnapshot =
                assignment.ResidualRecipientCode,
            CropCycleCodeSnapshot = profitability.CropCycleCode,
            CropCycleNameSnapshot = profitability.CropCycleName,
            CommodityIdSnapshot = profitability.CommodityIdSnapshot,
            CommodityCodeSnapshot =
                profitability.CommodityCodeSnapshot,
            CommodityNameSnapshot =
                profitability.CommodityNameSnapshot,
            RecognizedRevenue = profitability.RecognizedRevenue,
            CollectedRevenue = profitability.CollectedRevenue,
            OutstandingReceivable =
                profitability.OutstandingReceivable,
            ActivityResourceCost =
                profitability.ActivityResourceCost,
            ManualExpenseCost = profitability.ManualExpenseCost,
            TotalCultivationCost =
                profitability.TotalCultivationCost,
            NetProfit = profitability.NetProfit,
            Outcome = profitability.Outcome,
            ConfirmedInvestorCapital =
                profitability.ConfirmedInvestorCapital,
            ConfirmedPartnerCapital =
                profitability.ConfirmedPartnerCapital,
            TotalConfirmedCapital =
                profitability.TotalConfirmedCapital,
            AvailableHarvestQuantity =
                profitability.AvailableHarvestQuantity,
            TotalCapital = calculation.TotalCapital,
            TotalCapitalRecovery = calculation.TotalCapitalRecovery,
            TotalCapitalLoss = calculation.TotalCapitalLoss,
            TotalManagementProfitShare =
                calculation.TotalManagementProfitShare,
            TotalReturnOnCapitalProfitShare =
                calculation.TotalReturnOnCapitalProfitShare,
            TotalPriorityProfitShare =
                calculation.TotalPriorityProfitShare,
            TotalResidualProfitShare =
                calculation.TotalResidualProfitShare,
            TotalProfitShare = calculation.TotalProfitShare,
            TotalPayout = calculation.TotalPayout,
            CalculationVersion = NormalizeRequiredText(
                calculation.CalculationVersion,
                MaxCalculationVersionLength,
                nameof(calculation),
                "Calculation version"),
            CalculatedAt = profitability.GeneratedAt,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            Status = ProfitSharingWaterfallSettlementStatus.Finalized,
            FinalizedAt = finalizedAt,
            CreatedAt = finalizedAt
        };

        foreach (var allocation in calculation.PriorityAllocations
                     .OrderBy(item => item.Sequence))
        {
            settlement._priorityAllocations.Add(
                ProfitSharingWaterfallPriorityAllocation.Create(
                    organizationId,
                    settlement.Id,
                    allocation,
                    finalizedAt));
        }

        foreach (var allocation in calculation.Allocations
                     .OrderBy(item => item.Sequence))
        {
            settlement._participantAllocations.Add(
                ProfitSharingWaterfallParticipantAllocation.Create(
                    organizationId,
                    settlement.Id,
                    allocation,
                    finalizedAt));
        }

        foreach (var share in assignment.ResidualShares
                     .OrderBy(item => item.Sequence))
        {
            settlement._residualShares.Add(
                ProfitSharingWaterfallResidualShareSnapshot.Create(
                    organizationId,
                    settlement.Id,
                    share,
                    finalizedAt));
        }

        settlement.EnsureSnapshotInvariants();

        return settlement;
    }

    public void Void(string voidReason, DateTime voidedAt)
    {
        if (Status != ProfitSharingWaterfallSettlementStatus.Finalized)
        {
            throw new InvalidOperationException(
                "Only a finalized waterfall settlement can be voided.");
        }

        ValidateUtc(voidedAt, nameof(voidedAt));

        if (voidedAt < FinalizedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(voidedAt),
                "Void time cannot be earlier than finalization time.");
        }

        VoidReason = NormalizeRequiredText(
            voidReason,
            MaxVoidReasonLength,
            nameof(voidReason),
            "Void reason");
        Status = ProfitSharingWaterfallSettlementStatus.Voided;
        VoidedAt = voidedAt;
        UpdatedAt = voidedAt;
    }

    private static void ValidateSourceIdentity(
        Guid organizationId,
        Guid cropCycleId,
        ProfitSharingSchemeAssignment assignment,
        CropCycleProfitabilityReport profitability,
        ProfitSharingWaterfallCalculationResult calculation)
    {
        if (assignment.OrganizationId != organizationId ||
            profitability.OrganizationId != organizationId ||
            calculation.OrganizationId != organizationId)
        {
            throw new ArgumentException(
                "All finalization sources must belong to the same organization.");
        }

        if (assignment.CropCycleId != cropCycleId ||
            profitability.CropCycleId != cropCycleId ||
            calculation.CropCycleId != cropCycleId)
        {
            throw new ArgumentException(
                "All finalization sources must belong to the same crop cycle.");
        }
    }

    private static void ValidateFinalizationReadiness(
        CropCycleProfitabilityReport profitability)
    {
        if (profitability.OutstandingReceivable != 0)
        {
            throw new InvalidOperationException(
                "Waterfall settlement cannot be finalized while revenue remains uncollected.");
        }

        if (profitability.AvailableHarvestQuantity != 0)
        {
            throw new InvalidOperationException(
                "Waterfall settlement cannot be finalized while harvest remains unsold.");
        }

        if (profitability.TotalCultivationCost <= 0)
        {
            throw new InvalidOperationException(
                "Waterfall settlement requires a positive cultivation cost.");
        }

        if (profitability.TotalConfirmedCapital !=
            profitability.TotalCultivationCost)
        {
            throw new InvalidOperationException(
                "Confirmed capital must equal cultivation cost before finalization.");
        }
    }

    private static void ValidateCalculationMatchesReport(
        CropCycleProfitabilityReport profitability,
        ProfitSharingWaterfallCalculationResult calculation)
    {
        if (calculation.RecognizedRevenue !=
                profitability.RecognizedRevenue ||
            calculation.TotalCultivationCost !=
                profitability.TotalCultivationCost ||
            calculation.NetProfit != profitability.NetProfit ||
            calculation.Outcome != profitability.Outcome ||
            calculation.TotalCapital !=
                profitability.TotalConfirmedCapital ||
            calculation.CalculationVersion !=
                ProfitSharingWaterfallCalculator
                    .CurrentCalculationVersion)
        {
            throw new InvalidOperationException(
                "Waterfall calculation does not match the profitability snapshot.");
        }
    }

    private static void ValidateCalculationMatchesAssignment(
        ProfitSharingSchemeAssignment assignment,
        ProfitSharingWaterfallCalculationResult calculation)
    {
        if (assignment.ResidualMethod != calculation.ResidualMethod ||
            assignment.Participants.Count != calculation.Allocations.Count ||
            assignment.PriorityRules.Count !=
                calculation.PriorityAllocations.Count)
        {
            throw new InvalidOperationException(
                "Waterfall calculation does not match the assigned scheme snapshot.");
        }

        var allocationsByCode = calculation.Allocations.ToDictionary(
            allocation => allocation.ParticipantCodeSnapshot,
            StringComparer.Ordinal);

        foreach (var participant in assignment.Participants)
        {
            if (!allocationsByCode.TryGetValue(
                    participant.ParticipantCode,
                    out var allocation) ||
                allocation.ParticipantNameSnapshot !=
                    participant.ParticipantName ||
                allocation.ParticipantRole !=
                    participant.ParticipantRole ||
                allocation.ParticipatesInResidualProfit !=
                    participant.ParticipatesInResidualProfit ||
                allocation.Sequence != participant.Sequence)
            {
                throw new InvalidOperationException(
                    "Participant allocation does not match the assigned scheme snapshot.");
            }
        }

        var priorityByCode = calculation.PriorityAllocations.ToDictionary(
            allocation => allocation.RuleCode,
            StringComparer.Ordinal);

        var participantNamesByCode = assignment.Participants.ToDictionary(
            participant => participant.ParticipantCode,
            participant => participant.ParticipantName,
            StringComparer.Ordinal);

        foreach (var rule in assignment.PriorityRules)
        {
            if (!priorityByCode.TryGetValue(
                    rule.RuleCode,
                    out var allocation) ||
                allocation.RuleType != rule.RuleType ||
                allocation.RecipientCodeSnapshot != rule.RecipientCode ||
                allocation.RecipientNameSnapshot !=
                    participantNamesByCode[rule.RecipientCode] ||
                allocation.Rate.Numerator != rule.RateNumerator ||
                allocation.Rate.Denominator != rule.RateDenominator ||
                allocation.Sequence != rule.Sequence)
            {
                throw new InvalidOperationException(
                    "Priority allocation does not match the assigned scheme snapshot.");
            }
        }
    }

    private void EnsureSnapshotInvariants()
    {
        if (_participantAllocations.Count == 0)
        {
            throw new InvalidOperationException(
                "Final settlement must contain participant allocations.");
        }

        if (_participantAllocations.Sum(item => item.ConfirmedCapital) !=
                TotalCapital ||
            _participantAllocations.Sum(item => item.CapitalRecovery) !=
                TotalCapitalRecovery ||
            _participantAllocations.Sum(item => item.CapitalLoss) !=
                TotalCapitalLoss ||
            _participantAllocations.Sum(item => item.TotalProfitShare) !=
                TotalProfitShare ||
            _participantAllocations.Sum(item => item.TotalPayout) !=
                TotalPayout)
        {
            throw new InvalidOperationException(
                "Participant allocation totals do not match the final settlement.");
        }

        if (_participantAllocations.Any(item =>
                item.CapitalRecovery + item.CapitalLoss !=
                    item.ConfirmedCapital ||
                item.ManagementProfitShare +
                    item.ReturnOnCapitalProfitShare +
                    item.ResidualProfitShare !=
                    item.TotalProfitShare ||
                item.CapitalRecovery + item.TotalProfitShare !=
                    item.TotalPayout))
        {
            throw new InvalidOperationException(
                "Participant allocation components are inconsistent.");
        }

        var managementPriorityTotal = _priorityAllocations
            .Where(item =>
                item.RuleType ==
                    ProfitSharingPriorityRuleType.ManagementShare)
            .Sum(item => item.AllocatedAmount);

        var returnOnCapitalPriorityTotal = _priorityAllocations
            .Where(item =>
                item.RuleType ==
                    ProfitSharingPriorityRuleType.ReturnOnCapital)
            .Sum(item => item.AllocatedAmount);

        if (_priorityAllocations.Any(item =>
                item.BaseAmount < 0 ||
                item.RequestedAmount < 0 ||
                item.AllocatedAmount < 0 ||
                item.UnallocatedAmount < 0 ||
                item.AllocatedAmount + item.UnallocatedAmount !=
                    item.RequestedAmount))
        {
            throw new InvalidOperationException(
                "Priority allocation components are inconsistent.");
        }

        if (_priorityAllocations.Sum(item => item.AllocatedAmount) !=
                TotalPriorityProfitShare ||
            managementPriorityTotal !=
                TotalManagementProfitShare ||
            returnOnCapitalPriorityTotal !=
                TotalReturnOnCapitalProfitShare ||
            _participantAllocations.Sum(
                item => item.ManagementProfitShare) !=
                TotalManagementProfitShare ||
            _participantAllocations.Sum(
                item => item.ReturnOnCapitalProfitShare) !=
                TotalReturnOnCapitalProfitShare ||
            _participantAllocations.Sum(
                item => item.ResidualProfitShare) !=
                TotalResidualProfitShare ||
            TotalCapitalRecovery + TotalCapitalLoss != TotalCapital ||
            TotalPriorityProfitShare + TotalResidualProfitShare !=
                TotalProfitShare ||
            TotalPayout != RecognizedRevenue)
        {
            throw new InvalidOperationException(
                "Final waterfall settlement totals are inconsistent.");
        }

        EnsureUniqueContiguousSequence(
            _participantAllocations.Select(item => item.Sequence),
            "Participant allocation");

        if (_priorityAllocations.Count > 0)
        {
            EnsureUniqueContiguousSequence(
                _priorityAllocations.Select(item => item.Sequence),
                "Priority allocation");
        }

        if (_residualShares.Count > 0)
        {
            EnsureUniqueContiguousSequence(
                _residualShares.Select(item => item.Sequence),
                "Residual share");
        }
    }

    private static void EnsureUniqueContiguousSequence(
        IEnumerable<int> sequences,
        string displayName)
    {
        var ordered = sequences.OrderBy(value => value).ToArray();

        for (var index = 0; index < ordered.Length; index++)
        {
            if (ordered[index] != index + 1)
            {
                throw new InvalidOperationException(
                    $"{displayName} sequence must be contiguous from one.");
            }
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string displayName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateSettlementDate(DateOnly settlementDate)
    {
        if (settlementDate == default)
        {
            throw new ArgumentException(
                "Settlement date must be provided.",
                nameof(settlementDate));
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be a non-default UTC value.",
                parameterName);
        }
    }

    private static string NormalizeCode(string code)
    {
        var normalized = NormalizeRequiredText(
                code,
                MaxCodeLength,
                nameof(code),
                "Settlement code")
            .ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Settlement code may only contain letters, numbers, hyphens, and underscores.",
                nameof(code));
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
