namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingResidualPolicyInput
{
    private ProfitSharingResidualPolicyInput(
        ProfitSharingResidualMethod method,
        string? recipientCode,
        IReadOnlyCollection<ProfitSharingResidualShareInput>
            fixedShares)
    {
        Method = method;
        RecipientCode = recipientCode;
        FixedShares = fixedShares;
    }

    public ProfitSharingResidualMethod Method { get; }

    public string? RecipientCode { get; }

    public IReadOnlyCollection<ProfitSharingResidualShareInput>
        FixedShares { get; }

    public static ProfitSharingResidualPolicyInput
        RemainderToParticipant(string recipientCode)
    {
        return new ProfitSharingResidualPolicyInput(
            ProfitSharingResidualMethod
                .RemainderToParticipant,
            recipientCode,
            Array.Empty<ProfitSharingResidualShareInput>());
    }

    public static ProfitSharingResidualPolicyInput
        ProRataCapital()
    {
        return new ProfitSharingResidualPolicyInput(
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            Array.Empty<ProfitSharingResidualShareInput>());
    }

    public static ProfitSharingResidualPolicyInput
        FixedPercentage(
            IReadOnlyCollection<
                ProfitSharingResidualShareInput> shares)
    {
        ArgumentNullException.ThrowIfNull(shares);

        return new ProfitSharingResidualPolicyInput(
            ProfitSharingResidualMethod.FixedPercentage,
            null,
            shares);
    }
}
