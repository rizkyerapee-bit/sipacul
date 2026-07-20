using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.ValueObjects;

namespace SiPacul.Domain.Entities.MasterData;

public sealed class Commodity : AggregateRoot
{
    private Commodity()
    {
    }

    public CommodityCode Code { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? ScientificName { get; private set; }

    public string? Description { get; private set; }

    public Guid CommodityCategoryId { get; private set; }

    public CommodityCategory CommodityCategory { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    public static Commodity Create(
        CommodityCode code,
        string name,
        Guid commodityCategoryId,
        string? scientificName,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Commodity name cannot be empty.");

        return new Commodity
        {
            Code = code,
            Name = name.Trim(),
            CommodityCategoryId = commodityCategoryId,
            ScientificName = scientificName?.Trim(),
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        Guid commodityCategoryId,
        string? scientificName,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Commodity name cannot be empty.");

        Name = name.Trim();
        CommodityCategoryId = commodityCategoryId;
        ScientificName = scientificName?.Trim();
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
