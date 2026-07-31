namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record UpdateCultivationActivityNotesRequest(
    string? Notes,
    string? IssueNotes);
