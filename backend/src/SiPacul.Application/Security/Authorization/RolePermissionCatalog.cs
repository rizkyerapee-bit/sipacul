using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Security.Authorization;

public static class RolePermissionCatalog
{
    private static readonly IReadOnlyList<string>
        OwnerPermissions = Permissions.All;

    private static readonly IReadOnlyList<string>
        AdminPermissions =
            Array.AsReadOnly(
                Permissions.All
                    .Where(permission =>
                        permission !=
                            Permissions.MembersAssignOwner)
                    .ToArray());

    private static readonly IReadOnlyList<string>
        FinancePermissions =
            Array.AsReadOnly(
                new[]
                {
                    Permissions.OrganizationsRead,
                    Permissions.MembersRead,
                    Permissions.MasterDataRead,
                    Permissions.LandsRead,
                    Permissions.CultivationRead,
                    Permissions.HarvestRead,
                    Permissions.SalesRead,
                    Permissions.SalesWrite,
                    Permissions.FinanceRead,
                    Permissions.FinanceWrite,
                    Permissions.ProfitSharingRead,
                    Permissions.ProfitSharingWrite,
                    Permissions.ProfitSharingFinalize,
                    Permissions.ProfitSharingVoid,
                    Permissions.AuditRead
                });

    private static readonly IReadOnlyList<string>
        OperatorPermissions =
            Array.AsReadOnly(
                new[]
                {
                    Permissions.OrganizationsRead,
                    Permissions.MasterDataRead,
                    Permissions.LandsRead,
                    Permissions.CultivationRead,
                    Permissions.CultivationWrite,
                    Permissions.HarvestRead,
                    Permissions.HarvestWrite,
                    Permissions.SalesRead,
                    Permissions.SalesWrite
                });

    public static IReadOnlyList<string> GetPermissions(
        OrganizationRole role)
    {
        return role switch
        {
            OrganizationRole.Owner =>
                OwnerPermissions,

            OrganizationRole.Admin =>
                AdminPermissions,

            OrganizationRole.Finance =>
                FinancePermissions,

            OrganizationRole.Operator =>
                OperatorPermissions,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(role),
                    role,
                    "Organization role is not supported.")
        };
    }

    public static bool HasPermission(
        OrganizationRole role,
        string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return GetPermissions(role).Contains(
            permission.Trim(),
            StringComparer.Ordinal);
    }
}
