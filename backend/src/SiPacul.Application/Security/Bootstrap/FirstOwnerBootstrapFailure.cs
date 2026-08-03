namespace SiPacul.Application.Security.Bootstrap;

public enum FirstOwnerBootstrapFailure
{
    None = 0,

    NotConfigured = 1,

    InvalidToken = 2,

    AlreadyInitialized = 3,

    InvalidRequest = 4,

    IdentityValidationFailed = 5,

    Conflict = 6,

    PersistenceFailure = 7
}
