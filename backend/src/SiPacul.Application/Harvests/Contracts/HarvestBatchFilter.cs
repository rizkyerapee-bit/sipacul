using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Harvests.Contracts;

public sealed record HarvestBatchFilter(
    HarvestBatchStatus? Status = null,
    DateOnly? HarvestDateFrom = null,
    DateOnly? HarvestDateTo = null,
    HarvestQuantityUnit? QuantityUnit = null,
    string? QualityGrade = null);
