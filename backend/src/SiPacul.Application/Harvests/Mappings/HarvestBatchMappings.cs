using SiPacul.Application.Harvests.Contracts;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Harvests.Mappings;

public static class HarvestBatchMappings
{
    public static HarvestBatchResponse ToResponse(
        this HarvestBatch harvestBatch,
        decimal confirmedSoldQuantity = 0)
    {
        ArgumentNullException.ThrowIfNull(harvestBatch);

        var normalizedSold =
            Math.Round(
                Math.Max(
                    confirmedSoldQuantity,
                    0),
                4,
                MidpointRounding.AwayFromZero);

        var availableQuantity =
            harvestBatch.Status ==
                HarvestBatchStatus.Confirmed
                ? Math.Round(
                    Math.Max(
                        harvestBatch.NetQuantity -
                            normalizedSold,
                        0),
                    4,
                    MidpointRounding.AwayFromZero)
                : 0;

        return new HarvestBatchResponse(
            harvestBatch.Id,
            harvestBatch.OrganizationId,
            harvestBatch.CropCycleId,
            harvestBatch.Code,
            harvestBatch.HarvestDate,
            harvestBatch.GrossQuantity,
            harvestBatch.RejectedQuantity,
            harvestBatch.NetQuantity,
            harvestBatch.QuantityUnit,
            harvestBatch.QualityGrade,
            harvestBatch.StorageLocation,
            harvestBatch.Notes,
            harvestBatch.Status,
            harvestBatch.ConfirmedAt,
            harvestBatch.CancellationReason,
            normalizedSold,
            availableQuantity,
            harvestBatch.CreatedAt,
            harvestBatch.UpdatedAt);
    }
}
