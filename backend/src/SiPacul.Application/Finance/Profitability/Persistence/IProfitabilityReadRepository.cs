namespace SiPacul.Application.Finance.Profitability.Persistence;

public interface IProfitabilityReadRepository
{
    Task<ProfitabilitySourceSnapshot?> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);
}
