using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Sops.Services;

public interface ICultivationSopService
{
    Task<Result<CultivationSopResponse>> CreateAsync(
        Guid organizationId,
        CreateCultivationSopRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CultivationSopResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid? commodityId = null,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> UpdateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        UpdateCultivationSopRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> ActivateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> DeactivateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> AddStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        AddCultivationSopStepRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> UpdateStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        UpdateCultivationSopStepRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> RemoveStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationSopResponse>> MoveStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        MoveCultivationSopStepRequest request,
        CancellationToken cancellationToken = default);
}
