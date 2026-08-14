using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments.Services;

public interface IProfitSharingSchemeAssignmentService
{
    Task<Result<ProfitSharingSchemeAssignmentResponse>> AssignAsync(
        Guid organizationId,
        Guid cropCycleId,
        AssignProfitSharingSchemeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSchemeAssignmentResponse>> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);
}
