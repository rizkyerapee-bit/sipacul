using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiPacul.Application.Organizations.Members;
using SiPacul.Application.Organizations.Members.Contracts;
using SiPacul.Application.Organizations.Members.Services;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using SiPacul.Shared.Results;

namespace SiPacul.Infrastructure.Identity;

public sealed class OrganizationMemberService :
    IOrganizationMemberService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SiPaculDbContext _dbContext;
    private readonly ILogger<OrganizationMemberService> _logger;

    public OrganizationMemberService(
        UserManager<ApplicationUser> userManager,
        SiPaculDbContext dbContext,
        ILogger<OrganizationMemberService> logger)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<
        Result<IReadOnlyList<OrganizationMemberResponse>>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        var organizationError =
            await ValidateOrganizationAsync(
                organizationId,
                cancellationToken);

        if (organizationError is not null)
        {
            return Result<
                IReadOnlyList<OrganizationMemberResponse>>
                .Failure(organizationError);
        }

        var members =
            await CreateMemberQuery(organizationId)
                .OrderBy(member => member.Role)
                .ThenBy(member => member.Email)
                .ThenBy(member => member.MembershipId)
                .ToArrayAsync(cancellationToken);

        return Result<
            IReadOnlyList<OrganizationMemberResponse>>
            .Success(members);
    }

    public async Task<Result<OrganizationMemberResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid membershipId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            membershipId);

        if (identifierError is not null)
        {
            return Result<OrganizationMemberResponse>
                .Failure(identifierError);
        }

        var member =
            await CreateMemberQuery(organizationId)
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.MembershipId == membershipId,
                    cancellationToken);

        return member is null
            ? Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.NotFound(
                    organizationId,
                    membershipId))
            : Result<OrganizationMemberResponse>.Success(
                member);
    }

    public async Task<Result<OrganizationMemberResponse>>
        CreateAsync(
            Guid organizationId,
            CreateOrganizationMemberRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateCreateRequest(
            organizationId,
            request);

        if (requestError is not null)
        {
            return Result<OrganizationMemberResponse>
                .Failure(requestError);
        }

        string normalizedEmail;

        try
        {
            normalizedEmail =
                ApplicationUser.Create(request.Email).Email!;
        }
        catch (ArgumentException exception)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.Validation(
                    exception.Message));
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
                                cancellationToken);

                    var organizationExists =
                        await _dbContext.Organizations
                            .AsNoTracking()
                            .AnyAsync(
                                organization =>
                                    organization.Id ==
                                        organizationId &&
                                    !organization.IsDeleted,
                                cancellationToken);

                    if (!organizationExists)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return Result<OrganizationMemberResponse>
                            .Failure(
                                OrganizationMemberErrors
                                    .OrganizationNotFound(
                                        organizationId));
                    }

                    var user =
                        await _userManager.FindByEmailAsync(
                            normalizedEmail);

                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (user is not null && !user.IsActive)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return Result<OrganizationMemberResponse>
                            .Failure(
                                OrganizationMemberErrors
                                    .UserInactive(
                                        normalizedEmail));
                    }

                    if (user is null)
                    {
                        if (string.IsNullOrWhiteSpace(
                                request.InitialPassword))
                        {
                            await transaction.RollbackAsync(
                                cancellationToken);

                            return Result<
                                OrganizationMemberResponse>
                                .Failure(
                                    OrganizationMemberErrors
                                        .Validation(
                                            "Initial password is " +
                                            "required for a new " +
                                            "user account."));
                        }

                        user = ApplicationUser.Create(
                            normalizedEmail);

                        var identityResult =
                            await _userManager.CreateAsync(
                                user,
                                request.InitialPassword);

                        cancellationToken
                            .ThrowIfCancellationRequested();

                        if (!identityResult.Succeeded)
                        {
                            await transaction.RollbackAsync(
                                cancellationToken);

                            return Result<
                                OrganizationMemberResponse>
                                .Failure(
                                    MapIdentityFailure(
                                        identityResult));
                        }
                    }

                    var membershipExists =
                        await _dbContext
                            .OrganizationMemberships
                            .AsNoTracking()
                            .AnyAsync(
                                membership =>
                                    membership.OrganizationId ==
                                        organizationId &&
                                    membership.UserId == user.Id,
                                cancellationToken);

                    if (membershipExists)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return Result<OrganizationMemberResponse>
                            .Failure(
                                OrganizationMemberErrors
                                    .AlreadyExists(
                                        normalizedEmail));
                    }

                    var membership =
                        OrganizationMembership.Create(
                            organizationId,
                            user.Id,
                            request.Role);

                    _dbContext.OrganizationMemberships.Add(
                        membership);

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    _logger.LogInformation(
                        "Organization member {MembershipId} " +
                        "created for user {UserId} in " +
                        "organization {OrganizationId} with " +
                        "role {Role}.",
                        membership.Id,
                        user.Id,
                        organizationId,
                        membership.Role);

                    return Result<OrganizationMemberResponse>
                        .Success(
                            ToResponse(
                                membership,
                                user));
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "Organization member creation encountered " +
                "a data conflict in organization " +
                "{OrganizationId}.",
                organizationId);

            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.DataConflict());
        }
    }

    public async Task<Result<OrganizationMemberResponse>>
        ChangeRoleAsync(
            Guid organizationId,
            Guid membershipId,
            UpdateOrganizationMemberRoleRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            membershipId);

        if (identifierError is not null)
        {
            return Result<OrganizationMemberResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.Validation(
                    "Member role request cannot be null."));
        }

        if (!Enum.IsDefined(request.Role))
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.Validation(
                    "Organization role is not supported."));
        }

        if (request.Role == OrganizationRole.Owner)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.OwnerProtected());
        }

        var membership = await FindMembershipForUpdateAsync(
            organizationId,
            membershipId,
            cancellationToken);

        if (membership is null)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.NotFound(
                    organizationId,
                    membershipId));
        }

        if (membership.Role == OrganizationRole.Owner)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.OwnerProtected());
        }

        var previousRole = membership.Role;
        membership.ChangeRole(request.Role);

        if (previousRole != membership.Role)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return await GetUpdatedResponseAsync(
            organizationId,
            membershipId,
            cancellationToken);
    }

    public Task<Result<OrganizationMemberResponse>>
        ActivateAsync(
            Guid organizationId,
            Guid membershipId,
            CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(
            organizationId,
            membershipId,
            true,
            cancellationToken);
    }

    public Task<Result<OrganizationMemberResponse>>
        SuspendAsync(
            Guid organizationId,
            Guid membershipId,
            CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(
            organizationId,
            membershipId,
            false,
            cancellationToken);
    }

    private async Task<Result<OrganizationMemberResponse>>
        SetStatusAsync(
            Guid organizationId,
            Guid membershipId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            membershipId);

        if (identifierError is not null)
        {
            return Result<OrganizationMemberResponse>.Failure(
                identifierError);
        }

        var membership = await FindMembershipForUpdateAsync(
            organizationId,
            membershipId,
            cancellationToken);

        if (membership is null)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.NotFound(
                    organizationId,
                    membershipId));
        }

        if (membership.Role == OrganizationRole.Owner)
        {
            return Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.OwnerProtected());
        }

        var wasActive = membership.IsActive;

        if (shouldBeActive)
        {
            membership.Activate();
        }
        else
        {
            membership.Suspend();
        }

        if (wasActive != membership.IsActive)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return await GetUpdatedResponseAsync(
            organizationId,
            membershipId,
            cancellationToken);
    }

    private IQueryable<OrganizationMemberResponse>
        CreateMemberQuery(Guid organizationId)
    {
        return
            from membership in _dbContext
                .OrganizationMemberships
                .AsNoTracking()
            join user in _dbContext.ApplicationUsers
                    .AsNoTracking()
                on membership.UserId equals user.Id
            where membership.OrganizationId ==
                    organizationId &&
                !membership.IsDeleted
            select new OrganizationMemberResponse(
                membership.Id,
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.EmailConfirmed,
                user.IsActive,
                membership.Role,
                membership.Status,
                membership.JoinedAt,
                membership.SuspendedAt);
    }

    private async Task<OrganizationMembership?>
        FindMembershipForUpdateAsync(
            Guid organizationId,
            Guid membershipId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.OrganizationMemberships
            .SingleOrDefaultAsync(
                membership =>
                    membership.OrganizationId ==
                        organizationId &&
                    membership.Id == membershipId &&
                    !membership.IsDeleted,
                cancellationToken);
    }

    private async Task<Result<OrganizationMemberResponse>>
        GetUpdatedResponseAsync(
            Guid organizationId,
            Guid membershipId,
            CancellationToken cancellationToken)
    {
        var response =
            await CreateMemberQuery(organizationId)
                .SingleAsync(
                    member =>
                        member.MembershipId == membershipId,
                    cancellationToken);

        return Result<OrganizationMemberResponse>.Success(
            response);
    }

    private async Task<Error?> ValidateOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            return OrganizationMemberErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        var exists =
            await _dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(
                    organization =>
                        organization.Id == organizationId &&
                        !organization.IsDeleted,
                    cancellationToken);

        return exists
            ? null
            : OrganizationMemberErrors
                .OrganizationNotFound(organizationId);
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid membershipId)
    {
        if (organizationId == Guid.Empty)
        {
            return OrganizationMemberErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (membershipId == Guid.Empty)
        {
            return OrganizationMemberErrors.Validation(
                "Membership identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateCreateRequest(
        Guid organizationId,
        CreateOrganizationMemberRequest request)
    {
        if (organizationId == Guid.Empty)
        {
            return OrganizationMemberErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (request is null)
        {
            return OrganizationMemberErrors.Validation(
                "Member request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return OrganizationMemberErrors.Validation(
                "Member email is required.");
        }

        if (request.InitialPassword is not null &&
            request.InitialPassword.Length >
                CreateOrganizationMemberRequest
                    .MaxInitialPasswordLength)
        {
            return OrganizationMemberErrors.Validation(
                "Initial password is too long.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            return OrganizationMemberErrors.Validation(
                "Organization role is not supported.");
        }

        if (request.Role == OrganizationRole.Owner)
        {
            return OrganizationMemberErrors.OwnerProtected();
        }

        return null;
    }

    private static Error MapIdentityFailure(
        IdentityResult identityResult)
    {
        var errors = identityResult.Errors.ToArray();

        if (errors.Any(error =>
                error.Code is
                    "DuplicateEmail" or
                    "DuplicateUserName"))
        {
            return OrganizationMemberErrors.DataConflict();
        }

        var descriptions =
            errors
                .Select(error => error.Description)
                .Where(description =>
                    !string.IsNullOrWhiteSpace(description))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        var message = descriptions.Length == 0
            ? "User account validation failed."
            : string.Join(" ", descriptions);

        return OrganizationMemberErrors.IdentityValidation(
            message);
    }

    private static OrganizationMemberResponse ToResponse(
        OrganizationMembership membership,
        ApplicationUser user)
    {
        return new OrganizationMemberResponse(
            membership.Id,
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.EmailConfirmed,
            user.IsActive,
            membership.Role,
            membership.Status,
            membership.JoinedAt,
            membership.SuspendedAt);
    }
}
