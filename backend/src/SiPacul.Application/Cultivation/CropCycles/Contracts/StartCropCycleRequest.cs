namespace SiPacul.Application.Cultivation.CropCycles.Contracts;

public sealed record StartCropCycleRequest(
    DateOnly ActualStartDate);
