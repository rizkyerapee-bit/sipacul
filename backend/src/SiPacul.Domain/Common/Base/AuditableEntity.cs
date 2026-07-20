namespace SiPacul.Domain.Common.Base;

/// <summary>
/// Menjadi kelas dasar bagi entity yang membutuhkan informasi audit
/// dan dukungan soft delete.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// Membuat entity auditable baru.
    /// </summary>
    protected AuditableEntity()
    {
    }

    /// <summary>
    /// Membuat entity auditable menggunakan identifier tertentu.
    /// </summary>
    /// <param name="id">Identifier entity.</param>
    protected AuditableEntity(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Waktu ketika entity dibuat dalam format UTC.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier pengguna yang membuat entity.
    /// </summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>
    /// Waktu terakhir entity diperbarui dalam format UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Identifier pengguna yang terakhir memperbarui entity.
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Waktu ketika entity dihapus secara logis.
    /// </summary>
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Identifier pengguna yang menghapus entity secara logis.
    /// </summary>
    public string? DeletedBy { get; protected set; }

    /// <summary>
    /// Menunjukkan apakah entity telah dihapus secara logis.
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Mengisi informasi audit ketika entity pertama kali dibuat.
    /// </summary>
    /// <param name="userId">Identifier pengguna pembuat.</param>
    public void MarkAsCreated(string? userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Mengisi informasi audit ketika entity diperbarui.
    /// </summary>
    /// <param name="userId">Identifier pengguna yang memperbarui.</param>
    public void MarkAsUpdated(string? userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Menghapus entity secara logis tanpa menghapus record dari database.
    /// </summary>
    /// <param name="userId">Identifier pengguna yang menghapus.</param>
    public void SoftDelete(string? userId)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Memulihkan entity yang sebelumnya dihapus secara logis.
    /// </summary>
    /// <param name="userId">Identifier pengguna yang memulihkan.</param>
    public void Restore(string? userId)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;

        MarkAsUpdated(userId);
    }

    private static string? NormalizeUserId(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : userId.Trim();
    }
}
