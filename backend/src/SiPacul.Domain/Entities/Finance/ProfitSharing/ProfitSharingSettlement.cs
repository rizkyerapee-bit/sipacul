using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public sealed partial class ProfitSharingSettlement :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxManagingPartnerCodeLength = 40;

    public const int MaxManagingPartnerNameLength = 150;

    public const int MaxCalculationVersionLength = 50;

    public const int MaxVoidReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private readonly List<ProfitSharingAllocation>
        _allocations = [];

    private ProfitSharingSettlement()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly SettlementDate { get; private set; }

    public string ManagingPartnerCode
    {
        get;
        private set;
    } = string.Empty;

    public string ManagingPartnerName
    {
        get;
        private set;
    } = string.Empty;

    public decimal RecognizedRevenue { get; private set; }

    public decimal CollectedRevenue { get; private set; }

    public decimal OutstandingReceivable =>
        RoundMoney(
            RecognizedRevenue - CollectedRevenue);

    public decimal ActivityResourceCost { get; private set; }

    public decimal ManualExpenseCost { get; private set; }

    public decimal TotalCultivationCost { get; private set; }

    public decimal NetProfit { get; private set; }

    public ProfitabilityOutcome Outcome { get; private set; }

    public decimal ManagementProfitPool { get; private set; }

    public decimal CapitalProfitPool { get; private set; }

    public decimal TotalInvestorCapital { get; private set; }

    public decimal TotalPartnerCapital { get; private set; }

    public decimal TotalCapital { get; private set; }

    public decimal TotalCapitalRecovery { get; private set; }

    public decimal TotalCapitalLoss { get; private set; }

    public decimal TotalInvestorProfitShare
    {
        get;
        private set;
    }

    public decimal TotalPartnerProfitShare
    {
        get;
        private set;
    }

    public decimal TotalPayout { get; private set; }

    public string CalculationVersion
    {
        get;
        private set;
    } = string.Empty;

    public string? Notes { get; private set; }

    public ProfitSharingSettlementStatus Status
    {
        get;
        private set;
    } = ProfitSharingSettlementStatus.Draft;

    public DateTime? FinalizedAt { get; private set; }

    public DateTime? VoidedAt { get; private set; }

    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<ProfitSharingAllocation>
        Allocations =>
            _allocations.AsReadOnly();

    public bool IsActive =>
        Status == ProfitSharingSettlementStatus.Finalized;

    public static ProfitSharingSettlement CreateDraft(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly settlementDate,
        string managingPartnerCode,
        string managingPartnerName,
        ProfitSharingCalculationResult calculation,
        string? notes)
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

        ArgumentNullException.ThrowIfNull(calculation);

        if (calculation.OrganizationId != organizationId)
        {
            throw new ArgumentException(
                "Calculation organization does not match " +
                "the settlement organization.",
                nameof(calculation));
        }

        if (calculation.CropCycleId != cropCycleId)
        {
            throw new ArgumentException(
                "Calculation crop cycle does not match " +
                "the settlement crop cycle.",
                nameof(calculation));
        }

        var normalizedPartnerCode =
            NormalizeCode(
                managingPartnerCode,
                MaxManagingPartnerCodeLength,
                nameof(managingPartnerCode),
                "Managing partner code");

        var normalizedPartnerName =
            NormalizeRequiredText(
                managingPartnerName,
                MaxManagingPartnerNameLength,
                nameof(managingPartnerName),
                "Managing partner name");

        ValidateCalculation(
            calculation,
            normalizedPartnerCode,
            normalizedPartnerName);

        var settlement =
            new ProfitSharingSettlement
            {
                OrganizationId = organizationId,
                CropCycleId = cropCycleId,
                Code =
                    NormalizeCode(
                        code,
                        MaxCodeLength,
                        nameof(code),
                        "Settlement code"),
                SettlementDate = settlementDate,
                ManagingPartnerCode =
                    normalizedPartnerCode,
                ManagingPartnerName =
                    normalizedPartnerName,
                RecognizedRevenue =
                    calculation.RecognizedRevenue,
                CollectedRevenue =
                    calculation.CollectedRevenue,
                ActivityResourceCost =
                    calculation.ActivityResourceCost,
                ManualExpenseCost =
                    calculation.ManualExpenseCost,
                TotalCultivationCost =
                    calculation.TotalCultivationCost,
                NetProfit = calculation.NetProfit,
                Outcome = calculation.Outcome,
                ManagementProfitPool =
                    calculation.ManagementProfitPool,
                CapitalProfitPool =
                    calculation.CapitalProfitPool,
                TotalInvestorCapital =
                    calculation.TotalInvestorCapital,
                TotalPartnerCapital =
                    calculation.TotalPartnerCapital,
                TotalCapital =
                    calculation.TotalCapital,
                TotalCapitalRecovery =
                    calculation.TotalCapitalRecovery,
                TotalCapitalLoss =
                    calculation.TotalCapitalLoss,
                TotalInvestorProfitShare =
                    calculation.TotalInvestorProfitShare,
                TotalPartnerProfitShare =
                    calculation.TotalPartnerProfitShare,
                TotalPayout =
                    calculation.TotalPayout,
                CalculationVersion =
                    NormalizeRequiredText(
                        calculation.CalculationVersion,
                        MaxCalculationVersionLength,
                        nameof(calculation),
                        "Calculation version"),
                Notes =
                    NormalizeOptionalText(
                        notes,
                        MaxNotesLength,
                        nameof(notes)),
                Status =
                    ProfitSharingSettlementStatus.Draft
            };

        foreach (var allocationCalculation in
                 calculation.Allocations
                     .OrderBy(allocation =>
                         allocation.Sequence))
        {
            settlement._allocations.Add(
                ProfitSharingAllocation.Create(
                    organizationId,
                    settlement.Id,
                    allocationCalculation));
        }

        settlement.EnsureSnapshotInvariants();

        return settlement;
    }

    public void UpdateDraft(
        DateOnly settlementDate,
        string? notes)
    {
        EnsureStatus(
            ProfitSharingSettlementStatus.Draft,
            "Only a draft settlement can be updated.");

        ValidateSettlementDate(settlementDate);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (SettlementDate == settlementDate &&
            Notes == normalizedNotes)
        {
            return;
        }

        SettlementDate = settlementDate;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FinalizeSettlement()
    {
        EnsureStatus(
            ProfitSharingSettlementStatus.Draft,
            "Only a draft settlement can be finalized.");

        EnsureSnapshotInvariants();

        if (OutstandingReceivable != 0)
        {
            throw new InvalidOperationException(
                "Settlement cannot be finalized while related " +
                "revenue remains uncollected.");
        }

        var now = DateTime.UtcNow;

        Status =
            ProfitSharingSettlementStatus.Finalized;

        FinalizedAt = now;
        UpdatedAt = now;
    }

    public void Void(string voidReason)
    {
        if (Status is not
                ProfitSharingSettlementStatus.Draft and
            not ProfitSharingSettlementStatus.Finalized)
        {
            throw new InvalidOperationException(
                "Only a draft or finalized settlement can be voided.");
        }

        var now = DateTime.UtcNow;

        VoidReason =
            NormalizeRequiredText(
                voidReason,
                MaxVoidReasonLength,
                nameof(voidReason),
                "Void reason");

        Status = ProfitSharingSettlementStatus.Voided;
        VoidedAt = now;
        UpdatedAt = now;
    }

    public bool MatchesCalculation(
        ProfitSharingCalculationResult calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        if (OrganizationId != calculation.OrganizationId ||
            CropCycleId != calculation.CropCycleId ||
            RecognizedRevenue !=
                calculation.RecognizedRevenue ||
            CollectedRevenue !=
                calculation.CollectedRevenue ||
            ActivityResourceCost !=
                calculation.ActivityResourceCost ||
            ManualExpenseCost !=
                calculation.ManualExpenseCost ||
            TotalCultivationCost !=
                calculation.TotalCultivationCost ||
            NetProfit != calculation.NetProfit ||
            Outcome != calculation.Outcome ||
            ManagementProfitPool !=
                calculation.ManagementProfitPool ||
            CapitalProfitPool !=
                calculation.CapitalProfitPool ||
            TotalInvestorCapital !=
                calculation.TotalInvestorCapital ||
            TotalPartnerCapital !=
                calculation.TotalPartnerCapital ||
            TotalCapital != calculation.TotalCapital ||
            TotalCapitalRecovery !=
                calculation.TotalCapitalRecovery ||
            TotalCapitalLoss !=
                calculation.TotalCapitalLoss ||
            TotalInvestorProfitShare !=
                calculation.TotalInvestorProfitShare ||
            TotalPartnerProfitShare !=
                calculation.TotalPartnerProfitShare ||
            TotalPayout != calculation.TotalPayout ||
            CalculationVersion !=
                calculation.CalculationVersion ||
            _allocations.Count !=
                calculation.Allocations.Count)
        {
            return false;
        }

        var calculationBySequence =
            calculation.Allocations
                .OrderBy(allocation =>
                    allocation.Sequence)
                .ToArray();

        var settlementBySequence =
            _allocations
                .OrderBy(allocation =>
                    allocation.Sequence)
                .ToArray();

        for (
            var index = 0;
            index < settlementBySequence.Length;
            index++)
        {
            if (!settlementBySequence[index].Matches(
                    calculationBySequence[index]))
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureSnapshotInvariants()
    {
        if (_allocations.Count == 0)
        {
            throw new InvalidOperationException(
                "Settlement must contain at least one allocation.");
        }

        if (_allocations.Any(allocation =>
                allocation.OrganizationId !=
                    OrganizationId ||
                allocation.ProfitSharingSettlementId !=
                    Id))
        {
            throw new InvalidOperationException(
                "Settlement allocation ownership is inconsistent.");
        }

        var orderedAllocations =
            _allocations
                .OrderBy(allocation =>
                    allocation.Sequence)
                .ToArray();

        var expectedSequence =
            Enumerable.Range(
                1,
                orderedAllocations.Length);

        if (!orderedAllocations
            .Select(allocation =>
                allocation.Sequence)
            .SequenceEqual(expectedSequence))
        {
            throw new InvalidOperationException(
                "Settlement allocation sequence must be contiguous.");
        }

        if (orderedAllocations
            .GroupBy(allocation =>
                new
                {
                    allocation.ContributorRole,
                    allocation.ContributorCodeSnapshot
                })
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Settlement allocation identities must be unique.");
        }

        if (RoundMoney(
                orderedAllocations.Sum(allocation =>
                    allocation.ConfirmedCapital)) !=
            TotalCapital)
        {
            throw new InvalidOperationException(
                "Allocation capital must equal settlement capital.");
        }

        if (RoundMoney(
                orderedAllocations.Sum(allocation =>
                    allocation.CapitalRecovery)) !=
            TotalCapitalRecovery)
        {
            throw new InvalidOperationException(
                "Allocation recovery must equal settlement recovery.");
        }

        if (RoundMoney(
                orderedAllocations.Sum(allocation =>
                    allocation.CapitalLoss)) !=
            TotalCapitalLoss)
        {
            throw new InvalidOperationException(
                "Allocation loss must equal settlement loss.");
        }

        var investorProfitShare =
            RoundMoney(
                orderedAllocations
                    .Where(allocation =>
                        allocation.ContributorRole ==
                            CapitalContributorRole.Investor)
                    .Sum(allocation =>
                        allocation.TotalProfitShare));

        var partnerProfitShare =
            RoundMoney(
                orderedAllocations
                    .Where(allocation =>
                        allocation.ContributorRole ==
                            CapitalContributorRole.Partner)
                    .Sum(allocation =>
                        allocation.TotalProfitShare));

        if (investorProfitShare !=
            TotalInvestorProfitShare)
        {
            throw new InvalidOperationException(
                "Investor allocation profit share is inconsistent.");
        }

        if (partnerProfitShare !=
            TotalPartnerProfitShare)
        {
            throw new InvalidOperationException(
                "Partner allocation profit share is inconsistent.");
        }

        if (RoundMoney(
                orderedAllocations.Sum(allocation =>
                    allocation.TotalPayout)) !=
            TotalPayout)
        {
            throw new InvalidOperationException(
                "Allocation payout must equal settlement payout.");
        }

        if (TotalPayout != RecognizedRevenue)
        {
            throw new InvalidOperationException(
                "Settlement payout must equal recognized revenue.");
        }

        if (CollectedRevenue < 0 ||
            CollectedRevenue > RecognizedRevenue)
        {
            throw new InvalidOperationException(
                "Collected revenue must be between zero and " +
                "recognized revenue.");
        }

        var expectedNetProfit =
            RoundMoney(
                RecognizedRevenue -
                    TotalCultivationCost);

        if (NetProfit != expectedNetProfit)
        {
            throw new InvalidOperationException(
                "Settlement net profit is inconsistent.");
        }

        var expectedOutcome =
            NetProfit switch
            {
                < 0 => ProfitabilityOutcome.Loss,
                > 0 => ProfitabilityOutcome.Profit,
                _ => ProfitabilityOutcome.BreakEven
            };

        if (Outcome != expectedOutcome)
        {
            throw new InvalidOperationException(
                "Settlement outcome is inconsistent.");
        }
    }

    private static void ValidateCalculation(
        ProfitSharingCalculationResult calculation,
        string managingPartnerCode,
        string managingPartnerName)
    {
        if (calculation.CalculationVersion !=
            ProfitSharingCalculator.CurrentCalculationVersion)
        {
            throw new ArgumentException(
                "Calculation version is not supported for a " +
                "new settlement.",
                nameof(calculation));
        }

        var managingPartnerAllocation =
            calculation.Allocations
                .SingleOrDefault(allocation =>
                    allocation.ContributorRole ==
                        CapitalContributorRole.Partner &&
                    allocation.ContributorCodeSnapshot ==
                        managingPartnerCode);

        if (managingPartnerAllocation is null)
        {
            throw new ArgumentException(
                "Managing partner allocation is missing.",
                nameof(calculation));
        }

        if (managingPartnerAllocation
                .ContributorNameSnapshot !=
            managingPartnerName)
        {
            throw new ArgumentException(
                "Managing partner allocation identity does not " +
                "match the settlement snapshot.",
                nameof(calculation));
        }

        if (calculation.ManagementProfitPool > 0)
        {
            if (managingPartnerAllocation
                    .ManagementProfitShare !=
                calculation.ManagementProfitPool)
            {
                throw new ArgumentException(
                    "Managing partner management profit share " +
                    "is inconsistent.",
                    nameof(calculation));
            }

            if (calculation.Allocations
                .Where(allocation =>
                    allocation != managingPartnerAllocation)
                .Any(allocation =>
                    allocation.ManagementProfitShare != 0))
            {
                throw new ArgumentException(
                    "Management profit may only be assigned to " +
                    "the managing partner.",
                    nameof(calculation));
            }
        }
    }

    private void EnsureStatus(
        ProfitSharingSettlementStatus expectedStatus,
        string message)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(message);
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

    private static void ValidateSettlementDate(
        DateOnly settlementDate)
    {
        if (settlementDate == default)
        {
            throw new ArgumentException(
                "Settlement date must be provided.",
                nameof(settlementDate));
        }
    }

    private static string NormalizeCode(
        string value,
        int maximumLength,
        string parameterName,
        string displayName)
    {
        var normalized =
            NormalizeRequiredText(
                value,
                maximumLength,
                parameterName,
                displayName)
            .ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"{displayName} format is invalid.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        int maximumLength,
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

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    [GeneratedRegex(
        "^[A-Z0-9][A-Z0-9._-]{0,39}$")]
    private static partial Regex CodePattern();
}
