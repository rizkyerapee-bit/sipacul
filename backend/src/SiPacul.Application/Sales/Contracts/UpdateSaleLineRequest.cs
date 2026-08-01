namespace SiPacul.Application.Sales.Contracts;

public sealed record UpdateSaleLineRequest(
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscount,
    string? Notes);
