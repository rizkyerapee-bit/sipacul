using SiPacul.Domain.Common.Base;

namespace SiPacul.Domain.Entities.MasterData;

public sealed class CommodityCategory : AggregateRoot
{
    public const int MaxNameLength = 150;

    public const int MaxDescriptionLength = 500;

    private readonly List<Commodity> _commodities = [];

    private CommodityCategory()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Commodity> Commodities =>
        _commodities.AsReadOnly();

    public static CommodityCategory Create(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Category name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        if (description?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        return new CommodityCategory
        {
            Name = normalizedName,
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Category name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        if (description?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        Name = normalizedName;
        Description = description?.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
