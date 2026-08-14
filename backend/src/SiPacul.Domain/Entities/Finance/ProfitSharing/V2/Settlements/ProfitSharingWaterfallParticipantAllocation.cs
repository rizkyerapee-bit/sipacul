namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

public sealed class ProfitSharingWaterfallParticipantAllocation
{
    private ProfitSharingWaterfallParticipantAllocation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingWaterfallSettlementId
    {
        get;
        private set;
    }

    public string ParticipantCodeSnapshot { get; private set; } =
        string.Empty;

    public string ParticipantNameSnapshot { get; private set; } =
        string.Empty;

    public ProfitSharingParticipantRole ParticipantRole
    {
        get;
        private set;
    }

    public decimal ConfirmedCapital { get; private set; }

    public decimal CapitalRatio { get; private set; }

    public bool ParticipatesInResidualProfit { get; private set; }

    public decimal CapitalRecovery { get; private set; }

    public decimal CapitalLoss { get; private set; }

    public decimal ManagementProfitShare { get; private set; }

    public decimal ReturnOnCapitalProfitShare { get; private set; }

    public decimal ResidualProfitShare { get; private set; }

    public decimal TotalProfitShare { get; private set; }

    public decimal TotalPayout { get; private set; }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingWaterfallParticipantAllocation Create(
        Guid organizationId,
        Guid settlementId,
        ProfitSharingWaterfallAllocationCalculation source,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingWaterfallParticipantAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingWaterfallSettlementId = settlementId,
            ParticipantCodeSnapshot = source.ParticipantCodeSnapshot,
            ParticipantNameSnapshot = source.ParticipantNameSnapshot,
            ParticipantRole = source.ParticipantRole,
            ConfirmedCapital = source.ConfirmedCapital,
            CapitalRatio = source.CapitalRatio,
            ParticipatesInResidualProfit =
                source.ParticipatesInResidualProfit,
            CapitalRecovery = source.CapitalRecovery,
            CapitalLoss = source.CapitalLoss,
            ManagementProfitShare = source.ManagementProfitShare,
            ReturnOnCapitalProfitShare =
                source.ReturnOnCapitalProfitShare,
            ResidualProfitShare = source.ResidualProfitShare,
            TotalProfitShare = source.TotalProfitShare,
            TotalPayout = source.TotalPayout,
            Sequence = source.Sequence,
            CreatedAt = createdAt
        };
    }
}
