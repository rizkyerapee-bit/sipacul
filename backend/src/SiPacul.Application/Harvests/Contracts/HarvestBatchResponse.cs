using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Harvests.Contracts;

public sealed record HarvestBatchResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    string Code,
    DateOnly HarvestDate,
    decimal GrossQuantity,
    decimal RejectedQuantity,
    decimal NetQuantity,
    HarvestQuantityUnit QuantityUnit,
    string? QualityGrade,
    string? StorageLocation,
    string? Notes,
    HarvestBatchStatus Status,
    DateTime? ConfirmedAt,
    string? CancellationReason,
    decimal ConfirmedSoldQuantity,
    decimal AvailableQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
