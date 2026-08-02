using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public sealed class ProfitSharingAllocation :
    IOrganizationOwned
{
    public const int MaxContributorCodeLength = 40;

    public const int MaxContributorNameLength = 150;

    private ProfitSharingAllocation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSettlementId
    {
        get;
        private set;
    }

    public string ContributorCodeSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public string ContributorNameSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public CapitalContributorRole ContributorRole
    {
        get;
        private set;
    }

    public decimal ConfirmedCapital { get; private set; }

    public decimal CapitalRatio { get; private set; }

    public decimal CapitalRecovery { get; private set; }

    public decimal CapitalLoss { get; private set; }

    public decimal ManagementProfitShare
    {
        get;
        private set;
    }

    public decimal CapitalProfitShare { get; private set; }

    public decimal TotalProfitShare { get; private set; }

    public decimal TotalPayout { get; private set; }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingAllocation Create(
        Guid organizationId,
        Guid profitSharingSettlementId,
        ProfitSharingAllocationCalculation calculation)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            profitSharingSettlementId,
            nameof(profitSharingSettlementId),
            "Profit sharing settlement");

        ArgumentNullException.ThrowIfNull(calculation);

        ValidateContributorRole(
            calculation.ContributorRole);

        var confirmedCapital =
            NormalizeNonNegativeMoney(
                calculation.ConfirmedCapital,
                nameof(calculation.ConfirmedCapital));

        var capitalRatio =
            NormalizeCapitalRatio(
                calculation.CapitalRatio);

        var capitalRecovery =
            NormalizeNonNegativeMoney(
                calculation.CapitalRecovery,
                nameof(calculation.CapitalRecovery));

        var capitalLoss =
            NormalizeNonNegativeMoney(
                calculation.CapitalLoss,
                nameof(calculation.CapitalLoss));

        var managementProfitShare =
            NormalizeNonNegativeMoney(
                calculation.ManagementProfitShare,
                nameof(calculation.ManagementProfitShare));

        var capitalProfitShare =
            NormalizeNonNegativeMoney(
                calculation.CapitalProfitShare,
                nameof(calculation.CapitalProfitShare));

        var totalProfitShare =
            NormalizeNonNegativeMoney(
                calculation.TotalProfitShare,
                nameof(calculation.TotalProfitShare));

        var totalPayout =
            NormalizeNonNegativeMoney(
                calculation.TotalPayout,
                nameof(calculation.TotalPayout));

        if (capitalRecovery > confirmedCapital)
        {
            throw new ArgumentException(
                "Capital recovery cannot exceed confirmed capital.",
                nameof(calculation));
        }

        if (RoundMoney(
                capitalRecovery + capitalLoss) !=
            confirmedCapital)
        {
            throw new ArgumentException(
                "Capital recovery and loss must equal " +
                "confirmed capital.",
                nameof(calculation));
        }

        if (RoundMoney(
                managementProfitShare +
                    capitalProfitShare) !=
            totalProfitShare)
        {
            throw new ArgumentException(
                "Profit share components must equal total " +
                "profit share.",
                nameof(calculation));
        }

        if (RoundMoney(
                capitalRecovery +
                    totalProfitShare) !=
            totalPayout)
        {
            throw new ArgumentException(
                "Capital recovery and profit share must equal " +
                "total payout.",
                nameof(calculation));
        }

        if (calculation.Sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(calculation),
                "Allocation sequence must be greater than zero.");
        }

        return new ProfitSharingAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSettlementId =
                profitSharingSettlementId,
            ContributorCodeSnapshot =
                NormalizeRequiredText(
                    calculation.ContributorCodeSnapshot,
                    MaxContributorCodeLength,
                    nameof(calculation),
                    "Contributor code"),
            ContributorNameSnapshot =
                NormalizeRequiredText(
                    calculation.ContributorNameSnapshot,
                    MaxContributorNameLength,
                    nameof(calculation),
                    "Contributor name"),
            ContributorRole =
                calculation.ContributorRole,
            ConfirmedCapital = confirmedCapital,
            CapitalRatio = capitalRatio,
            CapitalRecovery = capitalRecovery,
            CapitalLoss = capitalLoss,
            ManagementProfitShare =
                managementProfitShare,
            CapitalProfitShare = capitalProfitShare,
            TotalProfitShare = totalProfitShare,
            TotalPayout = totalPayout,
            Sequence = calculation.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal bool Matches(
        ProfitSharingAllocationCalculation calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        return
            ContributorCodeSnapshot ==
                calculation.ContributorCodeSnapshot &&
            ContributorNameSnapshot ==
                calculation.ContributorNameSnapshot &&
            ContributorRole ==
                calculation.ContributorRole &&
            ConfirmedCapital ==
                calculation.ConfirmedCapital &&
            CapitalRatio ==
                calculation.CapitalRatio &&
            CapitalRecovery ==
                calculation.CapitalRecovery &&
            CapitalLoss ==
                calculation.CapitalLoss &&
            ManagementProfitShare ==
                calculation.ManagementProfitShare &&
            CapitalProfitShare ==
                calculation.CapitalProfitShare &&
            TotalProfitShare ==
                calculation.TotalProfitShare &&
            TotalPayout ==
                calculation.TotalPayout &&
            Sequence ==
                calculation.Sequence;
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

    private static void ValidateContributorRole(
        CapitalContributorRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Contributor role is unsupported.");
        }
    }

    private static decimal NormalizeNonNegativeMoney(
        decimal value,
        string parameterName)
    {
        var normalized = RoundMoney(value);

        if (normalized < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Money value cannot be negative.");
        }

        return normalized;
    }

    private static decimal NormalizeCapitalRatio(
        decimal value)
    {
        var normalized =
            Math.Round(
                value,
                8,
                MidpointRounding.AwayFromZero);

        if (normalized < 0 ||
            normalized > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Capital ratio must be between zero and one.");
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
}
