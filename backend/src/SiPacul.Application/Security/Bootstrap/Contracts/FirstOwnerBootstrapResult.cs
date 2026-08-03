namespace SiPacul.Application.Security.Bootstrap.Contracts;

public sealed record FirstOwnerBootstrapResult(
    FirstOwnerBootstrapResponse? Value,
    FirstOwnerBootstrapFailure Failure,
    string? Message,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess =>
        Failure == FirstOwnerBootstrapFailure.None &&
        Value is not null;

    public static FirstOwnerBootstrapResult Success(
        FirstOwnerBootstrapResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new FirstOwnerBootstrapResult(
            value,
            FirstOwnerBootstrapFailure.None,
            null,
            Array.Empty<string>());
    }

    public static FirstOwnerBootstrapResult Failed(
        FirstOwnerBootstrapFailure failure,
        string? message = null,
        IReadOnlyList<string>? errors = null)
    {
        if (failure == FirstOwnerBootstrapFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed bootstrap result must specify " +
                "a failure.");
        }

        return new FirstOwnerBootstrapResult(
            null,
            failure,
            message,
            errors ?? Array.Empty<string>());
    }
}
