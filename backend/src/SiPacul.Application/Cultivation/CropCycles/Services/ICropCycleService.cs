using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.CropCycles.Services;

public interface ICropCycleService
{
    Task<Result<CropCycleResponse>> CreateAsync(
        Guid organizationId,
        CreateCropCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CropCycleResponse>>> GetAllAsync(
        Guid organizationId,
        CropCycleFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> UpdatePlanAsync(
        Guid organizationId,
        Guid cropCycleId,
        UpdateCropCyclePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> StartAsync(
        Guid organizationId,
        Guid cropCycleId,
        StartCropCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> CompleteAsync(
        Guid organizationId,
        Guid cropCycleId,
        CompleteCropCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancelCropCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CropCycleResponse>> UpdateNotesAsync(
        Guid organizationId,
        Guid cropCycleId,
        UpdateCropCycleNotesRequest request,
        CancellationToken cancellationToken = default);
}
