using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;

public interface IProfitSharingSchemeAssignmentRepository
{
    Task<ProfitSharingSchemeAssignment?> GetByCropCycleAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingSchemeAssignment?>
        GetByCropCycleForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default);

    void Add(ProfitSharingSchemeAssignment assignment);
}
