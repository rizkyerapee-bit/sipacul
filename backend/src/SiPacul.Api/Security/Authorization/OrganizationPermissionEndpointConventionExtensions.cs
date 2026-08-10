using SiPacul.Api.Common.Http;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Security.Authorization;

public static class
    OrganizationPermissionEndpointConventionExtensions
{
    private static readonly HashSet<string>
        CookieAntiforgeryPermissions =
            new(StringComparer.Ordinal)
            {
                Permissions.OrganizationsManage,
                Permissions.MasterDataWrite,
                Permissions.LandsWrite,
                Permissions.CultivationWrite,
                Permissions.HarvestWrite,
                Permissions.SalesWrite,
                Permissions.FinanceWrite,
                Permissions.ProfitSharingWrite,
                Permissions.ProfitSharingFinalize,
                Permissions.ProfitSharingVoid
            };

    public static TBuilder
        RequireOrganizationPermission<TBuilder>(
            this TBuilder builder,
            string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RequireAuthorization(
            OrganizationPermissionPolicies
                .CreatePolicyName(permission));

        if (CookieAntiforgeryPermissions.Contains(
                permission))
        {
            builder.AddEndpointFilter<
                TBuilder,
                CookieAntiforgeryEndpointFilter>();
        }

        return builder;
    }
}
