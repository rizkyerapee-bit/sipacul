using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Application.Sales.Contracts;
using SiPacul.Application.Sales.Mappings;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Sales.Services;

public sealed class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;

    private readonly ISaleConfirmationProcessor
        _confirmationProcessor;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public SaleService(
        ISaleRepository saleRepository,
        ISaleConfirmationProcessor confirmationProcessor,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _confirmationProcessor = confirmationProcessor;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SaleResponse>> CreateAsync(
        Guid organizationId,
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            request,
            "Sale request cannot be null.");

        if (requestError is not null)
        {
            return Result<SaleResponse>.Failure(
                requestError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.OrganizationNotFound(
                    organizationId));
        }

        Sale sale;

        try
        {
            sale = Sale.Create(
                organizationId,
                request.Code,
                request.SaleDate,
                request.BuyerName,
                request.BuyerPhone,
                request.BuyerAddress,
                request.PaymentTerm,
                request.DueDate,
                0,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }

        if (await _saleRepository.CodeExistsAsync(
                organizationId,
                sale.Code,
                cancellationToken))
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.CodeAlreadyExists(
                    sale.Code));
        }

        _saleRepository.Add(sale);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            sale.ToResponse());
    }

    public async Task<Result<IReadOnlyList<SaleResponse>>>
        GetAllAsync(
            Guid organizationId,
            SaleFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateOrganizationId(organizationId);

        if (identifierError is not null)
        {
            return Result<IReadOnlyList<SaleResponse>>
                .Failure(identifierError);
        }

        filter ??= new SaleFilter();

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<IReadOnlyList<SaleResponse>>
                .Failure(filterError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<IReadOnlyList<SaleResponse>>
                .Failure(
                    SaleErrors.OrganizationNotFound(
                        organizationId));
        }

        var sales = await _saleRepository.GetAllAsync(
            organizationId,
            filter.Status,
            filter.SaleDateFrom,
            filter.SaleDateTo,
            filter.PaymentTerm,
            NormalizeBuyerFilter(filter.BuyerName),
            cancellationToken);

        IReadOnlyList<SaleResponse> responses =
            sales
                .Select(sale => sale.ToResponse())
                .ToArray();

        return Result<IReadOnlyList<SaleResponse>>
            .Success(responses);
    }

    public async Task<Result<SaleResponse>> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return Result<SaleResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.OrganizationNotFound(
                    organizationId));
        }

        var sale = await _saleRepository.GetByIdAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (sale is null)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.NotFound(saleId));
        }

        return Result<SaleResponse>.Success(
            sale.ToResponse());
    }

    public async Task<Result<SaleResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid saleId,
        UpdateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            request,
            "Sale update request cannot be null.");

        if (requestError is not null)
        {
            return Result<SaleResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleForUpdateAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                saleResult.Error);
        }

        try
        {
            saleResult.Value.UpdateDraft(
                request.SaleDate,
                request.BuyerName,
                request.BuyerPhone,
                request.BuyerAddress,
                request.PaymentTerm,
                request.DueDate,
                request.DiscountAmount,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            saleResult.Value.ToResponse());
    }

    public async Task<Result<SaleResponse>> AddLineAsync(
        Guid organizationId,
        Guid saleId,
        AddSaleLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            request,
            "Sale line request cannot be null.");

        if (requestError is not null)
        {
            return Result<SaleResponse>.Failure(
                requestError);
        }

        if (request.HarvestBatchId == Guid.Empty)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    "Harvest batch identifier cannot be empty."));
        }

        var saleResult = await GetSaleForUpdateAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                saleResult.Error);
        }

        var referenceResult =
            await GetValidatedHarvestReferenceAsync(
                organizationId,
                request.HarvestBatchId,
                request.QuantityUnit,
                request.Quantity,
                cancellationToken);

        if (referenceResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                referenceResult.Error);
        }

        try
        {
            var reference = referenceResult.Value;

            saleResult.Value.AddLine(
                reference.HarvestBatchId,
                reference.HarvestBatchCode,
                reference.CropCycleId,
                reference.CropCycleCode,
                reference.CommodityId,
                reference.CommodityCode,
                reference.CommodityName,
                reference.QualityGrade,
                request.Quantity,
                request.QuantityUnit,
                request.UnitPrice,
                request.LineDiscount,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            saleResult.Value.ToResponse());
    }

    public async Task<Result<SaleResponse>> UpdateLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        UpdateSaleLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            saleLineId,
            request,
            "Sale line update request cannot be null.");

        if (requestError is not null)
        {
            return Result<SaleResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleForUpdateAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                saleResult.Error);
        }

        var line = saleResult.Value.Lines
            .SingleOrDefault(candidate =>
                candidate.Id == saleLineId);

        if (line is null)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.LineNotFound(
                    saleId,
                    saleLineId));
        }

        var referenceResult =
            await GetValidatedHarvestReferenceAsync(
                organizationId,
                line.HarvestBatchId,
                line.QuantityUnit,
                request.Quantity,
                cancellationToken);

        if (referenceResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                referenceResult.Error);
        }

        try
        {
            saleResult.Value.UpdateLine(
                saleLineId,
                request.Quantity,
                request.UnitPrice,
                request.LineDiscount,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            saleResult.Value.ToResponse());
    }

    public async Task<Result<SaleResponse>> RemoveLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId,
            saleLineId);

        if (identifierError is not null)
        {
            return Result<SaleResponse>.Failure(
                identifierError);
        }

        var saleResult = await GetSaleForUpdateAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                saleResult.Error);
        }

        if (!saleResult.Value.Lines.Any(line =>
                line.Id == saleLineId))
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.LineNotFound(
                    saleId,
                    saleLineId));
        }

        try
        {
            saleResult.Value.RemoveLine(saleLineId);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            saleResult.Value.ToResponse());
    }

    public async Task<Result<SaleResponse>> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return Result<SaleResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.OrganizationNotFound(
                    organizationId));
        }

        var result = await _confirmationProcessor
            .ConfirmAsync(
                organizationId,
                saleId,
                cancellationToken);

        if (result.IsSuccess)
        {
            return Result<SaleResponse>.Success(
                result.Sale!.ToResponse());
        }

        return Result<SaleResponse>.Failure(
            MapConfirmationFailure(
                saleId,
                result));
    }

    public async Task<Result<SaleResponse>> CancelAsync(
        Guid organizationId,
        Guid saleId,
        CancelSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            request,
            "Sale cancellation request cannot be null.");

        if (requestError is not null)
        {
            return Result<SaleResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleForUpdateAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleResponse>.Failure(
                saleResult.Error);
        }

        try
        {
            saleResult.Value.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaleResponse>.Failure(
                SaleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SaleResponse>.Success(
            saleResult.Value.ToResponse());
    }

    private async Task<Result<Sale>> GetSaleForUpdateAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<Sale>.Failure(
                SaleErrors.OrganizationNotFound(
                    organizationId));
        }

        var sale = await _saleRepository
            .GetByIdForUpdateAsync(
                organizationId,
                saleId,
                cancellationToken);

        if (sale is null)
        {
            return Result<Sale>.Failure(
                SaleErrors.NotFound(saleId));
        }

        return Result<Sale>.Success(sale);
    }

    private async Task<Result<SaleHarvestReference>>
        GetValidatedHarvestReferenceAsync(
            Guid organizationId,
            Guid harvestBatchId,
            HarvestQuantityUnit requestedUnit,
            decimal requestedQuantity,
            CancellationToken cancellationToken)
    {
        var reference = await _saleRepository
            .GetHarvestReferenceAsync(
                organizationId,
                harvestBatchId,
                cancellationToken);

        if (reference is null)
        {
            return Result<SaleHarvestReference>.Failure(
                SaleErrors.HarvestBatchNotFound(
                    harvestBatchId));
        }

        if (reference.Status !=
            HarvestBatchStatus.Confirmed)
        {
            return Result<SaleHarvestReference>.Failure(
                SaleErrors.HarvestBatchNotConfirmed(
                    harvestBatchId));
        }

        if (reference.QuantityUnit != requestedUnit)
        {
            return Result<SaleHarvestReference>.Failure(
                SaleErrors.QuantityUnitMismatch(
                    harvestBatchId,
                    reference.QuantityUnit,
                    requestedUnit));
        }

        var soldQuantity = await _saleRepository
            .GetConfirmedSoldQuantityAsync(
                organizationId,
                harvestBatchId,
                cancellationToken);

        var availableQuantity = Math.Round(
            Math.Max(
                reference.NetQuantity - soldQuantity,
                0),
            4,
            MidpointRounding.AwayFromZero);

        var normalizedRequestedQuantity =
            NormalizeQuantity(requestedQuantity);

        if (normalizedRequestedQuantity >
            availableQuantity)
        {
            return Result<SaleHarvestReference>.Failure(
                SaleErrors.InsufficientQuantity(
                    harvestBatchId,
                    normalizedRequestedQuantity,
                    availableQuantity));
        }

        return Result<SaleHarvestReference>.Success(
            reference);
    }

    private async Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository
            .GetByIdAsync(
                organizationId,
                cancellationToken);

        return organization is not null &&
            !organization.IsDeleted;
    }

    private static Error MapConfirmationFailure(
        Guid saleId,
        SaleConfirmationResult result)
    {
        return result.Failure switch
        {
            SaleConfirmationFailure.SaleNotFound =>
                SaleErrors.NotFound(saleId),

            SaleConfirmationFailure.InvalidStatus =>
                SaleErrors.InvalidStatusTransition(
                    result.Message ??
                    "Only a draft sale can be confirmed."),

            SaleConfirmationFailure.EmptySale =>
                SaleErrors.InvalidStatusTransition(
                    result.Message ??
                    "A sale must have at least one line " +
                    "before confirmation."),

            SaleConfirmationFailure.HarvestBatchNotFound =>
                SaleErrors.HarvestBatchNotFound(
                    result.HarvestBatchId ?? Guid.Empty),

            SaleConfirmationFailure
                .HarvestBatchNotConfirmed =>
                SaleErrors.HarvestBatchNotConfirmed(
                    result.HarvestBatchId ?? Guid.Empty),

            SaleConfirmationFailure.QuantityUnitMismatch =>
                SaleErrors.InvalidStatusTransition(
                    result.Message ??
                    "Sale line quantity unit does not match " +
                    "the harvest batch unit."),

            SaleConfirmationFailure.InsufficientQuantity =>
                SaleErrors.InsufficientQuantity(
                    result.HarvestBatchId ?? Guid.Empty,
                    result.RequestedQuantity,
                    result.AvailableQuantity),

            SaleConfirmationFailure.ConcurrencyConflict =>
                SaleErrors.ConfirmationConcurrency(),

            _ => SaleErrors.InvalidStatusTransition(
                result.Message ??
                "Sale confirmation failed.")
        };
    }

    private static Error? ValidateFilter(SaleFilter filter)
    {
        if (filter.SaleDateFrom.HasValue &&
            filter.SaleDateTo.HasValue &&
            filter.SaleDateFrom.Value >
                filter.SaleDateTo.Value)
        {
            return SaleErrors.Validation(
                "Sale date from cannot be after sale date to.");
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return SaleErrors.Validation(
                "Sale status filter is not supported.");
        }

        if (filter.PaymentTerm.HasValue &&
            !Enum.IsDefined(filter.PaymentTerm.Value))
        {
            return SaleErrors.Validation(
                "Sale payment term filter is not supported.");
        }

        return null;
    }

    private static string? NormalizeBuyerFilter(
        string? buyerName)
    {
        return string.IsNullOrWhiteSpace(buyerName)
            ? null
            : buyerName.Trim();
    }

    private static decimal NormalizeQuantity(
        decimal quantity)
    {
        return Math.Round(
            quantity,
            4,
            MidpointRounding.AwayFromZero);
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError =
            ValidateOrganizationId(organizationId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return SaleErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid saleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return SaleErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId,
            saleLineId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return SaleErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return SaleErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid saleId,
        Guid? saleLineId = null)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (saleId == Guid.Empty)
        {
            return SaleErrors.Validation(
                "Sale identifier cannot be empty.");
        }

        if (saleLineId.HasValue &&
            saleLineId.Value == Guid.Empty)
        {
            return SaleErrors.Validation(
                "Sale line identifier cannot be empty.");
        }

        return null;
    }
}
