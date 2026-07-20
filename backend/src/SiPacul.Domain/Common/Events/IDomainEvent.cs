namespace SiPacul.Domain.Common.Events;

/// <summary>
/// Menandai sebuah kejadian bisnis yang terjadi di dalam domain SiPacul.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Mendapatkan identitas unik domain event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Mendapatkan waktu ketika domain event terjadi dalam UTC.
    /// </summary>
    DateTime OccurredAtUtc { get; }
}
