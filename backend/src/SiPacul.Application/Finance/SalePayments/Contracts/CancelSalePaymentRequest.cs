namespace SiPacul.Application.Finance.SalePayments.Contracts;

public sealed record CancelSalePaymentRequest(
    string CancellationReason);
