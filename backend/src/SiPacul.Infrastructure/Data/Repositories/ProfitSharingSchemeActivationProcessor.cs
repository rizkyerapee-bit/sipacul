using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingSchemeActivationProcessor :
    IProfitSharingSchemeActivationProcessor
{
    private readonly SiPaculDbContext _dbContext;

    private readonly IProfitSharingSchemeRepository
        _schemeRepository;

    public ProfitSharingSchemeActivationProcessor(
        SiPaculDbContext dbContext,
        IProfitSharingSchemeRepository schemeRepository)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(schemeRepository);

        _dbContext = dbContext;
        _schemeRepository = schemeRepository;
    }

    public async Task<ProfitSharingSchemeActivationResult>
        ActivateAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken = default)
    {
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

                    var result =
                        await ActivateWithinTransactionAsync(
                            organizationId,
                            schemeId,
                            cancellationToken);

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return result;
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    return result;
                });
        }
        catch (Exception exception)
            when (IsConcurrencyConflict(exception))
        {
            return ProfitSharingSchemeActivationResult.Failed(
                ProfitSharingSchemeActivationFailure
                    .ConcurrencyConflict,
                "The scheme changed during activation. " +
                "Reload it and try again.");
        }
    }

    private async Task<ProfitSharingSchemeActivationResult>
        ActivateWithinTransactionAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken)
    {
        var scheme =
            await _schemeRepository.GetByIdForUpdateAsync(
                organizationId,
                schemeId,
                cancellationToken);

        if (scheme is null)
        {
            return ProfitSharingSchemeActivationResult.Failed(
                ProfitSharingSchemeActivationFailure
                    .SchemeNotFound);
        }

        if (scheme.Status != ProfitSharingSchemeStatus.Draft)
        {
            return ProfitSharingSchemeActivationResult.Failed(
                ProfitSharingSchemeActivationFailure
                    .InvalidStatus,
                "Only a draft scheme can be activated.");
        }

        var active =
            await _schemeRepository.GetActiveForUpdateAsync(
                organizationId,
                scheme.SchemeFamilyId,
                scheme.Id,
                cancellationToken);

        try
        {
            if (active is not null)
            {
                active.Supersede();

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            scheme.Activate();

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return ProfitSharingSchemeActivationResult.Failed(
                ProfitSharingSchemeActivationFailure
                    .InvalidStatus,
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ProfitSharingSchemeActivationResult.Failed(
                ProfitSharingSchemeActivationFailure
                    .InvalidStatus,
                exception.Message);
        }

        return ProfitSharingSchemeActivationResult.Succeeded(
            scheme);
    }

    private static bool IsConcurrencyConflict(
        Exception exception)
    {
        if (exception is PostgresException postgresException &&
            postgresException.SqlState is
                PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.UniqueViolation)
        {
            return true;
        }

        return exception.InnerException is not null &&
            IsConcurrencyConflict(exception.InnerException);
    }
}
