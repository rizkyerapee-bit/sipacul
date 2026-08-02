namespace SiPacul.Application.Security.Authorization;

public static class Permissions
{
    public const string OrganizationsRead =
        "organizations.read";

    public const string OrganizationsManage =
        "organizations.manage";

    public const string MembersRead =
        "members.read";

    public const string MembersManage =
        "members.manage";

    public const string MembersAssignOwner =
        "members.assign-owner";

    public const string MasterDataRead =
        "master-data.read";

    public const string MasterDataWrite =
        "master-data.write";

    public const string LandsRead =
        "lands.read";

    public const string LandsWrite =
        "lands.write";

    public const string CultivationRead =
        "cultivation.read";

    public const string CultivationWrite =
        "cultivation.write";

    public const string HarvestRead =
        "harvest.read";

    public const string HarvestWrite =
        "harvest.write";

    public const string SalesRead =
        "sales.read";

    public const string SalesWrite =
        "sales.write";

    public const string FinanceRead =
        "finance.read";

    public const string FinanceWrite =
        "finance.write";

    public const string ProfitSharingRead =
        "profit-sharing.read";

    public const string ProfitSharingWrite =
        "profit-sharing.write";

    public const string ProfitSharingFinalize =
        "profit-sharing.finalize";

    public const string ProfitSharingVoid =
        "profit-sharing.void";

    public const string AuditRead =
        "audit.read";

    public static IReadOnlyList<string> All { get; } =
        Array.AsReadOnly(
            new[]
            {
                OrganizationsRead,
                OrganizationsManage,
                MembersRead,
                MembersManage,
                MembersAssignOwner,
                MasterDataRead,
                MasterDataWrite,
                LandsRead,
                LandsWrite,
                CultivationRead,
                CultivationWrite,
                HarvestRead,
                HarvestWrite,
                SalesRead,
                SalesWrite,
                FinanceRead,
                FinanceWrite,
                ProfitSharingRead,
                ProfitSharingWrite,
                ProfitSharingFinalize,
                ProfitSharingVoid,
                AuditRead
            });
}
