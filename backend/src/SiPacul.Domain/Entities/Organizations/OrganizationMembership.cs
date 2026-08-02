using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Organizations;

public sealed class OrganizationMembership :
    AggregateRoot,
    IOrganizationOwned
{
    private OrganizationMembership()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public OrganizationMembershipStatus Status
    {
        get;
        private set;
    }

    public DateTime JoinedAt { get; private set; }

    public DateTime? SuspendedAt { get; private set; }

    public bool IsActive =>
        Status == OrganizationMembershipStatus.Active &&
        !IsDeleted;

    public static OrganizationMembership Create(
        Guid organizationId,
        Guid userId,
        OrganizationRole role)
    {
        ValidateOrganizationId(organizationId);
        ValidateUserId(userId);
        ValidateRole(role);

        return new OrganizationMembership
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            Status =
                OrganizationMembershipStatus.Active,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(OrganizationRole role)
    {
        ValidateRole(role);

        if (Role == role)
        {
            return;
        }

        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status ==
            OrganizationMembershipStatus.Suspended)
        {
            return;
        }

        Status =
            OrganizationMembershipStatus.Suspended;

        SuspendedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status ==
            OrganizationMembershipStatus.Active)
        {
            return;
        }

        Status =
            OrganizationMembershipStatus.Active;

        SuspendedAt = null;
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

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty.",
                nameof(userId));
        }
    }

    private static void ValidateRole(
        OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Organization role is not supported.");
        }
    }
}
