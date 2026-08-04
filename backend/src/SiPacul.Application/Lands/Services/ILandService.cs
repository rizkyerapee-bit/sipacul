using SiPacul.Application.Lands.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Lands.Services;

public interface ILandService
{
    Task<Result<LandResponse>> CreateAsync(
        Guid organizationId,
        CreateLandRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LandResponse>>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> GetByIdAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> UpdateAsync(
        Guid organizationId,
        Guid landId,
        UpdateLandRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> DeleteAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> ActivateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> DeactivateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> AddPlotAsync(
        Guid organizationId,
        Guid landId,
        AddLandPlotRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> UpdatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        UpdateLandPlotRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> RemovePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> ActivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default);

    Task<Result<LandResponse>> DeactivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default);
}
