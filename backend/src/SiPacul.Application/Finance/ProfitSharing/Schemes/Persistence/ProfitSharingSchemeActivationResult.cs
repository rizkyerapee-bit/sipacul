using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;

public enum ProfitSharingSchemeActivationFailure
{
    None = 0,
    SchemeNotFound = 1,
    InvalidStatus = 2,
    ConcurrencyConflict = 3
}

public sealed record ProfitSharingSchemeActivationResult
{
    private ProfitSharingSchemeActivationResult(
        bool isSuccess,
        ProfitSharingScheme? scheme,
        ProfitSharingSchemeActivationFailure failure,
        string? message)
    {
        IsSuccess = isSuccess;
        Scheme = scheme;
        Failure = failure;
        Message = message;
    }

    public bool IsSuccess { get; }

    public ProfitSharingScheme? Scheme { get; }

    public ProfitSharingSchemeActivationFailure Failure { get; }

    public string? Message { get; }

    public static ProfitSharingSchemeActivationResult Succeeded(
        ProfitSharingScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        return new ProfitSharingSchemeActivationResult(
            true,
            scheme,
            ProfitSharingSchemeActivationFailure.None,
            null);
    }

    public static ProfitSharingSchemeActivationResult Failed(
        ProfitSharingSchemeActivationFailure failure,
        string? message = null)
    {
        if (failure == ProfitSharingSchemeActivationFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed activation requires a failure reason.");
        }

        return new ProfitSharingSchemeActivationResult(
            false,
            null,
            failure,
            message);
    }
}
