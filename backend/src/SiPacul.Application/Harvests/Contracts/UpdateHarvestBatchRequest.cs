using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Harvests.Contracts;

public sealed record UpdateHarvestBatchRequest(
    DateOnly HarvestDate,
    decimal GrossQuantity,
    decimal RejectedQuantity,
    HarvestQuantityUnit QuantityUnit,
    string? QualityGrade,
    string? StorageLocation,
    string? Notes);
