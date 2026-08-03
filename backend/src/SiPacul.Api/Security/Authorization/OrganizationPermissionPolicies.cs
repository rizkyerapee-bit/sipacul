using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Security.Authorization;

public static class OrganizationPermissionPolicies
{
    public const string Prefix =
        "OrganizationPermission:";

    public const string OrganizationRouteValueName =
        "organizationId";

    public static string CreatePolicyName(
        string? permission)
    {
        var normalizedPermission =
            NormalizeKnownPermission(permission);

        return Prefix + normalizedPermission;
    }

    public static bool TryGetPermission(
        string? policyName,
        out string permission)
    {
        permission = string.Empty;

        if (string.IsNullOrWhiteSpace(policyName) ||
            !policyName.StartsWith(
                Prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        var candidate =
            policyName[Prefix.Length..];

        if (!Permissions.All.Contains(
                candidate,
                StringComparer.Ordinal))
        {
            return false;
        }

        permission = candidate;

        return true;
    }

    private static string NormalizeKnownPermission(
        string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException(
                "Permission is required.",
                nameof(permission));
        }

        var normalizedPermission =
            permission.Trim();

        if (!Permissions.All.Contains(
                normalizedPermission,
                StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(permission),
                permission,
                "Permission is not registered.");
        }

        return normalizedPermission;
    }
}
