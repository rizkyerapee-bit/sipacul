namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record ProfitSharingSchemeResidualShareRequest(
    string RecipientCode,
    decimal RateNumerator,
    decimal RateDenominator,
    int Sequence);
