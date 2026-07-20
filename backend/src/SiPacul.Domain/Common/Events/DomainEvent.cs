namespace SiPacul.Domain.Common.Events;

/// <summary>
/// Menjadi kelas dasar bagi seluruh domain event pada SiPacul.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Membuat domain event baru.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAtUtc = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; }
}
