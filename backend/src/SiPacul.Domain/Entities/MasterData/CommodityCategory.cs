using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.MasterData;

public sealed class CommodityCategory :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxNameLength = 150;

    public const int MaxDescriptionLength = 500;

    private readonly List<Commodity> _commodities = [];

    private CommodityCategory()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Commodity> Commodities =>
        _commodities.AsReadOnly();

    public static CommodityCategory Create(
        Guid organizationId,
        string name,
        string? description)
    {
        ValidateOrganizationId(organizationId);
        ValidateName(name);
        ValidateDescription(description);

        return new CommodityCategory
        {
            OrganizationId = organizationId,
            Name = name.Trim(),
            Description = NormalizeDescription(description)
        };
    }

    public void Update(
        string name,
        string? description)
    {
        ValidateName(name);
        ValidateDescription(description);

        var normalizedName = name.Trim();

        var normalizedDescription =
            NormalizeDescription(description);

        if (Name == normalizedName &&
            Description == normalizedDescription)
        {
            return;
        }

        Name = normalizedName;
        Description = normalizedDescription;

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

    private static void ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty.",
                nameof(organizationId));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name cannot be empty.",
                nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Category name cannot exceed " +
                $"{MaxNameLength} characters.",
                nameof(name));
        }
    }

    private static void ValidateDescription(
        string? description)
    {
        if (description?.Trim().Length >
            MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed " +
                $"{MaxDescriptionLength} characters.",
                nameof(description));
        }
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}
