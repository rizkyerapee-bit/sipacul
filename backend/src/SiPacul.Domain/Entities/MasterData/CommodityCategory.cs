using SiPacul.Domain.Common.Base;

namespace SiPacul.Domain.Entities.MasterData;

public sealed class CommodityCategory : AuditableEntity
{
    private CommodityCategory()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static CommodityCategory Create(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.");

        return new CommodityCategory
        {
            Name = name.Trim(),
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.");

        Name = name.Trim();
        Description = description?.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
