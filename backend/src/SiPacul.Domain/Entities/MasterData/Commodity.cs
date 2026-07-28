using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Events.MasterData;

namespace SiPacul.Domain.Entities.MasterData;

public sealed class Commodity :
    AggregateRoot,
    IOrganizationOwned
{
    private Commodity()
    {
    }

    public Guid OrganizationId { get; private set; }

    public CommodityCode Code { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? ScientificName { get; private set; }

    public string? Description { get; private set; }

    public Guid CommodityCategoryId { get; private set; }

    public CommodityCategory CommodityCategory { get; private set; } =
        null!;

    public bool IsActive { get; private set; } = true;

    public static Commodity Create(
        Guid organizationId,
        CommodityCode code,
        string name,
        Guid commodityCategoryId,
        string? scientificName,
        string? description)
    {
        ValidateOrganizationId(organizationId);
        ValidateCode(code);
        ValidateName(name);
        ValidateCommodityCategoryId(commodityCategoryId);

        var commodity = new Commodity
        {
            OrganizationId = organizationId,
            Code = code,
            Name = name.Trim(),
            CommodityCategoryId = commodityCategoryId,
            ScientificName = NormalizeOptionalText(
                scientificName),
            Description = NormalizeOptionalText(
                description)
        };

        commodity.AddDomainEvent(
            new CommodityCreatedDomainEvent(
                commodity.Id,
                commodity.Name,
                commodity.CommodityCategoryId));

        return commodity;
    }

    public void Update(
        string name,
        Guid commodityCategoryId,
        string? scientificName,
        string? description)
    {
        ValidateName(name);
        ValidateCommodityCategoryId(commodityCategoryId);

        var normalizedName = name.Trim();

        var normalizedScientificName =
            NormalizeOptionalText(scientificName);

        var normalizedDescription =
            NormalizeOptionalText(description);

        if (Name == normalizedName &&
            CommodityCategoryId == commodityCategoryId &&
            ScientificName == normalizedScientificName &&
            Description == normalizedDescription)
        {
            return;
        }

        Name = normalizedName;
        CommodityCategoryId = commodityCategoryId;
        ScientificName = normalizedScientificName;
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

    private static void ValidateCode(CommodityCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Commodity name cannot be empty.");
        }
    }

    private static void ValidateCommodityCategoryId(
        Guid commodityCategoryId)
    {
        if (commodityCategoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Commodity category identifier cannot be empty.",
                nameof(commodityCategoryId));
        }
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
