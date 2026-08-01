using SiPacul.Application.Sales.Contracts;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Mappings;

public static class SaleMappings
{
    public static SaleResponse ToResponse(this Sale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        IReadOnlyList<SaleLineResponse> lines =
            sale.Lines
                .OrderBy(line => line.CreatedAt)
                .ThenBy(line => line.Id)
                .Select(line => line.ToResponse())
                .ToArray();

        return new SaleResponse(
            sale.Id,
            sale.OrganizationId,
            sale.Code,
            sale.SaleDate,
            sale.BuyerName,
            sale.BuyerPhone,
            sale.BuyerAddress,
            sale.PaymentTerm,
            sale.DueDate,
            sale.DiscountAmount,
            sale.Subtotal,
            sale.TotalAmount,
            sale.Status,
            sale.ConfirmedAt,
            sale.CancellationReason,
            sale.Notes,
            lines,
            sale.CreatedAt,
            sale.UpdatedAt);
    }

    private static SaleLineResponse ToResponse(
        this SaleLine line)
    {
        return new SaleLineResponse(
            line.Id,
            line.HarvestBatchId,
            line.HarvestBatchCodeSnapshot,
            line.CropCycleIdSnapshot,
            line.CropCycleCodeSnapshot,
            line.CommodityIdSnapshot,
            line.CommodityCodeSnapshot,
            line.CommodityNameSnapshot,
            line.QualityGradeSnapshot,
            line.Quantity,
            line.QuantityUnit,
            line.UnitPrice,
            line.LineDiscount,
            line.LineTotal,
            line.Notes,
            line.CreatedAt,
            line.UpdatedAt);
    }
}
