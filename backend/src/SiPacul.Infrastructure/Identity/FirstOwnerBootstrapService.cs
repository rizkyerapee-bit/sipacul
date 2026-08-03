using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SiPacul.Application.Security.Bootstrap;
using SiPacul.Application.Security.Bootstrap.Contracts;
using SiPacul.Application.Security.Bootstrap.Services;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;

namespace SiPacul.Infrastructure.Identity;

public sealed class FirstOwnerBootstrapService :
    IFirstOwnerBootstrapService
{
    private const int MaximumSuppliedTokenLength = 4096;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SiPaculDbContext _dbContext;
    private readonly FirstOwnerBootstrapOptions _options;
    private readonly ILogger<FirstOwnerBootstrapService> _logger;

    public FirstOwnerBootstrapService(
        UserManager<ApplicationUser> userManager,
        SiPaculDbContext dbContext,
        IOptions<FirstOwnerBootstrapOptions> options,
        ILogger<FirstOwnerBootstrapService> logger)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _userManager = userManager;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FirstOwnerBootstrapStatusResponse>
        GetStatusAsync(
            CancellationToken cancellationToken = default)
    {
        var initialized =
            await HasBootstrapDataAsync(
                cancellationToken);

        return new FirstOwnerBootstrapStatusResponse(
            _options.IsConfigured,
            initialized,
            _options.IsConfigured && !initialized);
    }

    public async Task<FirstOwnerBootstrapResult>
        BootstrapAsync(
            FirstOwnerBootstrapRequest request,
            string? suppliedToken,
            CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.InvalidRequest,
                "Bootstrap request is required.");
        }

        if (!_options.IsConfigured)
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.NotConfigured,
                "First Owner bootstrap is not configured.");
        }

        if (!IsValidToken(suppliedToken))
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.InvalidToken,
                "Bootstrap authorization failed.");
        }

        var validationMessage =
            ValidateRequest(request);

        if (validationMessage is not null)
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.InvalidRequest,
                validationMessage);
        }

        var executionStrategy =
            _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(
                async () =>
                {
                    _dbContext.ChangeTracker.Clear();

                    await using var transaction =
                        await _dbContext.Database
                            .BeginTransactionAsync(
                                IsolationLevel.Serializable,
                                cancellationToken);

                    await _dbContext.Database
                        .ExecuteSqlRawAsync(
                            "LOCK TABLE \"Users\" " +
                            "IN SHARE ROW EXCLUSIVE MODE",
                            cancellationToken);

                    if (await HasBootstrapDataAsync(
                            cancellationToken))
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return FirstOwnerBootstrapResult.Failed(
                            FirstOwnerBootstrapFailure
                                .AlreadyInitialized,
                            "SiPacul has already been initialized.");
                    }

                    var user =
                        ApplicationUser.Create(
                            request.Email!);

                    var organization =
                        Organization.Create(
                            request.OrganizationCode!,
                            request.OrganizationName!,
                            request.OrganizationLegalName,
                            request.OrganizationTimeZone);

                    var identityResult =
                        await _userManager.CreateAsync(
                            user,
                            request.Password!);

                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (!identityResult.Succeeded)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return MapIdentityFailure(
                            identityResult);
                    }

                    var membership =
                        OrganizationMembership.Create(
                            organization.Id,
                            user.Id,
                            OrganizationRole.Owner);

                    _dbContext.Organizations.Add(
                        organization);

                    _dbContext.OrganizationMemberships.Add(
                        membership);

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    _logger.LogInformation(
                        "First Owner bootstrap completed for " +
                        "user {UserId}, organization " +
                        "{OrganizationId}, and membership " +
                        "{MembershipId}.",
                        user.Id,
                        organization.Id,
                        membership.Id);

                    return FirstOwnerBootstrapResult.Success(
                        new FirstOwnerBootstrapResponse(
                            user.Id,
                            user.Email!,
                            organization.Id,
                            organization.Code,
                            organization.Name,
                            membership.Id,
                            membership.Role,
                            user.CreatedAt));
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.InvalidRequest,
                exception.Message);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(
                exception))
        {
            _logger.LogWarning(
                "First Owner bootstrap encountered a " +
                "unique constraint conflict.");

            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.Conflict,
                "Bootstrap data conflicts with an " +
                "existing record.");
        }
        catch (Exception exception)
            when (IsConcurrencyFailure(exception))
        {
            _logger.LogWarning(
                "First Owner bootstrap encountered a " +
                "database concurrency conflict.");

            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.Conflict,
                "Bootstrap could not complete because " +
                "another initialization operation won.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "First Owner bootstrap failed.");

            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure
                    .PersistenceFailure,
                "Bootstrap could not be completed.");
        }
    }

    private async Task<bool> HasBootstrapDataAsync(
        CancellationToken cancellationToken)
    {
        if (await _dbContext.ApplicationUsers
            .AsNoTracking()
            .AnyAsync(cancellationToken))
        {
            return true;
        }

        if (await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(cancellationToken))
        {
            return true;
        }

        return await _dbContext.OrganizationMemberships
            .AsNoTracking()
            .AnyAsync(cancellationToken);
    }

    private bool IsValidToken(
        string? suppliedToken)
    {
        var expectedToken =
            _options.OwnerToken;

        if (string.IsNullOrWhiteSpace(expectedToken) ||
            string.IsNullOrWhiteSpace(suppliedToken) ||
            suppliedToken.Length >
                MaximumSuppliedTokenLength)
        {
            return false;
        }

        var expectedHash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    expectedToken));

        var suppliedHash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    suppliedToken));

        return CryptographicOperations.FixedTimeEquals(
            expectedHash,
            suppliedHash);
    }

    private static string? ValidateRequest(
        FirstOwnerBootstrapRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.OrganizationCode))
        {
            return "Organization code is required.";
        }

        if (string.IsNullOrWhiteSpace(
                request.OrganizationName))
        {
            return "Organization name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Owner email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Owner password is required.";
        }

        if (request.Password.Length >
            FirstOwnerBootstrapRequest.MaxPasswordLength)
        {
            return "Owner password is too long.";
        }

        return null;
    }

    private static FirstOwnerBootstrapResult
        MapIdentityFailure(
            IdentityResult identityResult)
    {
        var identityErrors =
            identityResult.Errors.ToArray();

        var duplicateExists =
            identityErrors.Any(error =>
                error.Code is
                    "DuplicateEmail" or
                    "DuplicateUserName");

        if (duplicateExists)
        {
            return FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure.Conflict,
                "Bootstrap data conflicts with an " +
                "existing account.");
        }

        var safeErrors =
            identityErrors
                .Select(error => error.Description)
                .Where(description =>
                    !string.IsNullOrWhiteSpace(
                        description))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        return FirstOwnerBootstrapResult.Failed(
            FirstOwnerBootstrapFailure
                .IdentityValidationFailed,
            "Owner account validation failed.",
            safeErrors);
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is
            PostgresException postgresException &&
            postgresException.SqlState ==
                PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsConcurrencyFailure(
        Exception exception)
    {
        var postgresException =
            exception as PostgresException ??
            exception.InnerException as PostgresException;

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }
}
