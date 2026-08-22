using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Application.Evaluations.SeasonHistories.Mappings;
using SiPacul.Application.Evaluations.SeasonHistories.Persistence;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonHistories.Services;

public sealed class SeasonHistoryService : ISeasonHistoryService
{
    public const int MaximumPageSize = 50;

    private readonly ISeasonHistoryReadRepository
        _readRepository;

    private readonly ILandRepository _landRepository;

    private readonly TimeProvider _timeProvider;

    public SeasonHistoryService(
        ISeasonHistoryReadRepository readRepository,
        ILandRepository landRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(landRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _readRepository = readRepository;
        _landRepository = landRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<LandSeasonHistoryResponse>> GetAsync(
        Guid organizationId,
        Guid landId,
        SeasonHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new SeasonHistoryFilter();

        var validationError = Validate(
            organizationId,
            landId,
            filter);

        if (validationError is not null)
        {
            return Failure(validationError);
        }

        var land = await _landRepository.GetByIdAsync(
            organizationId,
            landId,
            cancellationToken);

        if (land is null)
        {
            return Failure(
                SeasonHistoryErrors.LandNotFound(
                    organizationId,
                    landId));
        }

        LandPlot? filteredPlot = null;

        if (filter.LandPlotId.HasValue)
        {
            filteredPlot = land.Plots.SingleOrDefault(plot =>
                plot.Id == filter.LandPlotId.Value);

            if (filteredPlot is null)
            {
                return Failure(
                    SeasonHistoryErrors.LandPlotNotFound(
                        landId,
                        filter.LandPlotId.Value));
            }
        }

        SeasonHistoryPageSource sourcePage;

        try
        {
            sourcePage = await _readRepository.GetPageAsync(
                organizationId,
                landId,
                filter.LandPlotId,
                filter.IncludeNonTerminal,
                checked((filter.Page - 1) * filter.PageSize),
                filter.PageSize,
                cancellationToken);

            ValidateSourcePage(
                sourcePage,
                organizationId,
                land,
                filteredPlot,
                filter.PageSize);
        }
        catch (ArgumentException exception)
        {
            return SourceFailure(exception);
        }
        catch (InvalidOperationException exception)
        {
            return SourceFailure(exception);
        }
        catch (OverflowException exception)
        {
            return SourceFailure(exception);
        }

        var generatedAt = _timeProvider
            .GetUtcNow()
            .UtcDateTime;

        IReadOnlyList<SeasonEvaluationResponse> seasons;

        try
        {
            seasons = sourcePage.Cycles
                .Select(source =>
                {
                    var plot = land.Plots.Single(item =>
                        item.Id == source.LandPlotId);

                    return SeasonEvaluationCalculator
                        .Calculate(
                            source.ToInput(
                                land.Code,
                                land.Name,
                                plot.Code,
                                plot.Name,
                                generatedAt))
                        .ToResponse();
                })
                .ToArray();
        }
        catch (ArgumentException exception)
        {
            return SourceFailure(exception);
        }
        catch (InvalidOperationException exception)
        {
            return SourceFailure(exception);
        }
        catch (OverflowException exception)
        {
            return SourceFailure(exception);
        }

        var totalPages = sourcePage.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                (decimal)sourcePage.TotalCount /
                filter.PageSize);

        return Result<LandSeasonHistoryResponse>.Success(
            new LandSeasonHistoryResponse(
                organizationId,
                land.Id,
                land.Code,
                land.Name,
                filteredPlot?.Id,
                filteredPlot?.Code,
                filteredPlot?.Name,
                filter.IncludeNonTerminal,
                filter.Page,
                filter.PageSize,
                sourcePage.TotalCount,
                totalPages,
                filter.Page > 1,
                filter.Page < totalPages,
                seasons,
                generatedAt));
    }

    private static Error? Validate(
        Guid organizationId,
        Guid landId,
        SeasonHistoryFilter filter)
    {
        if (organizationId == Guid.Empty)
        {
            return SeasonHistoryErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (landId == Guid.Empty)
        {
            return SeasonHistoryErrors.Validation(
                "Land identifier cannot be empty.");
        }

        if (filter.LandPlotId == Guid.Empty)
        {
            return SeasonHistoryErrors.Validation(
                "Land plot identifier cannot be empty.");
        }

        if (filter.Page < 1)
        {
            return SeasonHistoryErrors.Validation(
                "Page must be at least one.");
        }

        if (filter.PageSize is < 1 or > MaximumPageSize)
        {
            return SeasonHistoryErrors.Validation(
                $"Page size must be between 1 and " +
                $"{MaximumPageSize}.");
        }

        return null;
    }

    private static void ValidateSourcePage(
        SeasonHistoryPageSource sourcePage,
        Guid organizationId,
        Land land,
        LandPlot? filteredPlot,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(sourcePage);
        ArgumentNullException.ThrowIfNull(sourcePage.Cycles);

        if (sourcePage.TotalCount < 0)
        {
            throw new InvalidOperationException(
                "Total season count cannot be negative.");
        }

        if (sourcePage.Cycles.Count > pageSize)
        {
            throw new InvalidOperationException(
                "Season source page exceeds requested page size.");
        }

        if (sourcePage.TotalCount < sourcePage.Cycles.Count)
        {
            throw new InvalidOperationException(
                "Total season count is below the returned row count.");
        }

        if (sourcePage.Cycles
            .Select(source => source.CropCycleId)
            .Distinct()
            .Count() != sourcePage.Cycles.Count)
        {
            throw new InvalidOperationException(
                "Season source contains duplicate crop cycles.");
        }

        foreach (var source in sourcePage.Cycles)
        {
            if (source.OrganizationId != organizationId ||
                source.LandId != land.Id)
            {
                throw new InvalidOperationException(
                    "Season source crossed organization or land scope.");
            }

            if (filteredPlot is not null &&
                source.LandPlotId != filteredPlot.Id)
            {
                throw new InvalidOperationException(
                    "Season source crossed the requested plot scope.");
            }

            if (!land.Plots.Any(plot =>
                    plot.Id == source.LandPlotId))
            {
                throw new InvalidOperationException(
                    $"Land plot '{source.LandPlotId}' from " +
                    "season source was not found.");
            }
        }
    }

    private static Result<LandSeasonHistoryResponse>
        SourceFailure(Exception exception)
    {
        return Failure(
            SeasonHistoryErrors.SourceDataInvalid(
                exception.Message));
    }

    private static Result<LandSeasonHistoryResponse>
        Failure(Error error)
    {
        return Result<LandSeasonHistoryResponse>
            .Failure(error);
    }
}
