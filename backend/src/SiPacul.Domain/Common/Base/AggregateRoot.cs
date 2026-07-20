using SiPacul.Domain.Common.Events;

namespace SiPacul.Domain.Common.Base;

/// <summary>
/// Menjadi kelas dasar bagi entity yang berperan sebagai aggregate root.
/// </summary>
public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Mendapatkan daftar domain event yang belum diproses.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    /// <summary>
    /// Menambahkan domain event baru ke aggregate.
    /// </summary>
    /// <param name="domainEvent">Domain event yang akan ditambahkan.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Menghapus seluruh domain event setelah berhasil diproses.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
