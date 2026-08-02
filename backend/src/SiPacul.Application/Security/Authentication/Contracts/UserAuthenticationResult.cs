namespace SiPacul.Application.Security.Authentication.Contracts;

public sealed record UserAuthenticationResult(
    bool Succeeded,
    CurrentUserResponse? User)
{
    public static UserAuthenticationResult Success(
        CurrentUserResponse user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserAuthenticationResult(
            true,
            user);
    }

    public static UserAuthenticationResult Failed()
    {
        return new UserAuthenticationResult(
            false,
            null);
    }
}
