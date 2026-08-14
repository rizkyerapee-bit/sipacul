namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;

public interface IProfitSharingSchemeActivationProcessor
{
    Task<ProfitSharingSchemeActivationResult> ActivateAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default);
}
