namespace SiPacul.Application.Harvests.Contracts;

public sealed record CancelHarvestBatchRequest(
    string CancellationReason);
