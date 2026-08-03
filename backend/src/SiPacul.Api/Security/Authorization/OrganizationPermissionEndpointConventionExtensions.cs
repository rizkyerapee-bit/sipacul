namespace SiPacul.Api.Security.Authorization;

public static class
    OrganizationPermissionEndpointConventionExtensions
{
    public static TBuilder
        RequireOrganizationPermission<TBuilder>(
            this TBuilder builder,
            string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            OrganizationPermissionPolicies
                .CreatePolicyName(permission));
    }
}
