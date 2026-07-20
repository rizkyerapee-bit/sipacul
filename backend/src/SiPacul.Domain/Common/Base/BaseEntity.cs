namespace SiPacul.Domain.Common.Base;

/// <summary>
/// Menjadi kelas dasar bagi seluruh entity pada domain SiPacul.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Membuat entity baru dengan identifier yang dibuat secara otomatis.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Membuat entity menggunakan identifier tertentu.
    /// Digunakan untuk proses materialisasi, migrasi data, atau pengujian.
    /// </summary>
    /// <param name="id">Identifier entity.</param>
    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity identifier tidak boleh kosong.",
                nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// Identifier unik entity.
    /// </summary>
    public Guid Id { get; protected set; }
}
