using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Finance.SalePayments.Mappings;

public static class SalePaymentMappings
{
    public static SalePaymentResponse ToResponse(
        this SalePayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new SalePaymentResponse(
            payment.Id,
            payment.OrganizationId,
            payment.SaleId,
            payment.Code,
            payment.PaymentDate,
            payment.Amount,
            payment.PaymentMethod,
            payment.ReferenceNumber,
            payment.ReceivedFrom,
            payment.Notes,
            payment.Status,
            payment.IsCollectedRevenue,
            payment.ConfirmedAt,
            payment.CancellationReason,
            payment.CreatedAt,
            payment.UpdatedAt);
    }

    public static SaleReceivableResponse ToReceivableResponse(
        this Sale sale,
        SaleReceivableSummary summary)
    {
        ArgumentNullException.ThrowIfNull(sale);

        return new SaleReceivableResponse(
            sale.Id,
            sale.Code,
            sale.SaleDate,
            sale.BuyerName,
            sale.PaymentTerm,
            sale.DueDate,
            summary.SaleTotalAmount,
            summary.ConfirmedPaidAmount,
            summary.OutstandingReceivable,
            summary.PaymentState,
            summary.IsFullyPaid,
            summary.HasCollectedRevenue);
    }
}
