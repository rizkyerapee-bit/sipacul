using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitSharingSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitSharingSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ManagingPartnerCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ManagingPartnerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RecognizedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CollectedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActivityResourceCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualExpenseCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCultivationCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ManagementProfitPool = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalProfitPool = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInvestorCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPartnerCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapitalRecovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapitalLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInvestorProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPartnerProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculationVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSettlements", x => x.Id);
                    table.UniqueConstraint("AK_ProfitSharingSettlements_Org_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfitSharingSettlements_CropCycle",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSettlements_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributorCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContributorNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContributorRole = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalRatio = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    CapitalRecovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ManagementProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingAllocations", x => x.Id);
                    table.UniqueConstraint("AK_ProfitSharingAllocations_Org_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfitSharingAllocations_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingAllocations_Settlement",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSettlementId },
                        principalTable: "ProfitSharingSettlements",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingAllocations_Org_Contributor",
                table: "ProfitSharingAllocations",
                columns: new[] { "OrganizationId", "ContributorRole", "ContributorCodeSnapshot" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingAllocations_Settlement_Contributor",
                table: "ProfitSharingAllocations",
                columns: new[] { "OrganizationId", "ProfitSharingSettlementId", "ContributorRole", "ContributorCodeSnapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingAllocations_Settlement_Sequence",
                table: "ProfitSharingAllocations",
                columns: new[] { "OrganizationId", "ProfitSharingSettlementId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSettlements_IsDeleted",
                table: "ProfitSharingSettlements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSettlements_Org_Cycle_Status",
                table: "ProfitSharingSettlements",
                columns: new[] { "OrganizationId", "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSettlements_Org_Date",
                table: "ProfitSharingSettlements",
                columns: new[] { "OrganizationId", "SettlementDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSettlements_Org_Cycle_Active",
                table: "ProfitSharingSettlements",
                columns: new[] { "OrganizationId", "CropCycleId" },
                unique: true,
                filter: "\"Status\" = 2 AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSettlements_Org_Cycle_Code",
                table: "ProfitSharingSettlements",
                columns: new[] { "OrganizationId", "CropCycleId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitSharingAllocations");

            migrationBuilder.DropTable(
                name: "ProfitSharingSettlements");
        }
    }
}
