namespace SiPacul.Infrastructure.Data;

public sealed class ProfitSharingSourceLockedException :
    InvalidOperationException
{
    public ProfitSharingSourceLockedException(
        string errorCode,
        string sourceType,
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId)
        : base(
            $"The {sourceType} source for crop cycle " +
            $"'{cropCycleId}' is locked by active finalized " +
            $"profit sharing settlement '{settlementId}'.")
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException(
                "Error code cannot be blank.",
                nameof(errorCode));
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException(
                "Source type cannot be blank.",
                nameof(sourceType));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty.",
                nameof(organizationId));
        }

        if (cropCycleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Crop cycle identifier cannot be empty.",
                nameof(cropCycleId));
        }

        if (settlementId == Guid.Empty)
        {
            throw new ArgumentException(
                "Settlement identifier cannot be empty.",
                nameof(settlementId));
        }

        ErrorCode = errorCode.Trim();
        SourceType = sourceType.Trim();
        OrganizationId = organizationId;
        CropCycleId = cropCycleId;
        SettlementId = settlementId;
    }

    public string ErrorCode { get; }

    public string SourceType { get; }

    public Guid OrganizationId { get; }

    public Guid CropCycleId { get; }

    public Guid SettlementId { get; }
}
