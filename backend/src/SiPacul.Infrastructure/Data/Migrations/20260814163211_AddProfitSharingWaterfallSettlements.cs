using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitSharingWaterfallSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitSharingWaterfallSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeFamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SchemeCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SchemeNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SchemeDescriptionSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SchemeVersionSnapshot = table.Column<int>(type: "integer", nullable: false),
                    SchemeAssignedAtSnapshot = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResidualMethod = table.Column<int>(type: "integer", nullable: false),
                    ResidualRecipientCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CropCycleCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CropCycleNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CommodityIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommodityNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RecognizedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CollectedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingReceivable = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActivityResourceCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualExpenseCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCultivationCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedInvestorCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfirmedPartnerCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalConfirmedCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableHarvestQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapitalRecovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapitalLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalManagementProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalReturnOnCapitalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPriorityProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalResidualProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculationVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ProfitSharingWaterfallSettlements", x => x.Id);
                    table.UniqueConstraint("AK_ProfitSharingWaterfallSettlements_Org_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfitSharingWaterfallSettlements_Assignment",
                        columns: x => new { x.OrganizationId, x.AssignmentId },
                        principalTable: "ProfitSharingSchemeAssignments",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingWaterfallSettlements_CropCycle",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingWaterfallSettlements_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingWaterfallSettlements_SourceScheme",
                        columns: x => new { x.OrganizationId, x.SourceSchemeId },
                        principalTable: "ProfitSharingSchemes",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingWaterfallParticipantAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingWaterfallSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ParticipantNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ParticipantRole = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedCapital = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalRatio = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    ParticipatesInResidualProfit = table.Column<bool>(type: "boolean", nullable: false),
                    CapitalRecovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ManagementProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnOnCapitalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ResidualProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalProfitShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingWaterfallParticipantAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PSWaterfallParticipantAlloc_Settlement",
                        columns: x => new { x.OrganizationId, x.ProfitSharingWaterfallSettlementId },
                        principalTable: "ProfitSharingWaterfallSettlements",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingWaterfallPriorityAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingWaterfallSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    RecipientCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecipientNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RateNumerator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateDenominator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnallocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingWaterfallPriorityAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PSWaterfallPriorityAlloc_Settlement",
                        columns: x => new { x.OrganizationId, x.ProfitSharingWaterfallSettlementId },
                        principalTable: "ProfitSharingWaterfallSettlements",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingWaterfallResidualShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingWaterfallSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RateNumerator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateDenominator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingWaterfallResidualShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PSWaterfallResidualShare_Settlement",
                        columns: x => new { x.OrganizationId, x.ProfitSharingWaterfallSettlementId },
                        principalTable: "ProfitSharingWaterfallSettlements",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallParticipantAllocations_OrganizationId~",
                table: "ProfitSharingWaterfallParticipantAllocations",
                columns: new[] { "OrganizationId", "ProfitSharingWaterfallSettlementId" });

            migrationBuilder.CreateIndex(
                name: "UX_PSWaterfallParticipantAlloc_Settlement_Participant",
                table: "ProfitSharingWaterfallParticipantAllocations",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "ParticipantCodeSnapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSWaterfallParticipantAlloc_Settlement_Sequence",
                table: "ProfitSharingWaterfallParticipantAllocations",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallPriorityAllocations_OrganizationId_Pr~",
                table: "ProfitSharingWaterfallPriorityAllocations",
                columns: new[] { "OrganizationId", "ProfitSharingWaterfallSettlementId" });

            migrationBuilder.CreateIndex(
                name: "UX_PSWaterfallPriorityAlloc_Settlement_Rule",
                table: "ProfitSharingWaterfallPriorityAllocations",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSWaterfallPriorityAlloc_Settlement_Sequence",
                table: "ProfitSharingWaterfallPriorityAllocations",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallResidualShares_OrganizationId_ProfitS~",
                table: "ProfitSharingWaterfallResidualShares",
                columns: new[] { "OrganizationId", "ProfitSharingWaterfallSettlementId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingWaterfallResidualShares_Settlement_Recipient",
                table: "ProfitSharingWaterfallResidualShares",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "RecipientCodeSnapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingWaterfallResidualShares_Settlement_Sequence",
                table: "ProfitSharingWaterfallResidualShares",
                columns: new[] { "ProfitSharingWaterfallSettlementId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallSettlements_IsDeleted",
                table: "ProfitSharingWaterfallSettlements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallSettlements_Org_Cycle_Status",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallSettlements_Org_Date",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "SettlementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallSettlements_OrganizationId_Assignment~",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingWaterfallSettlements_OrganizationId_SourceSche~",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "SourceSchemeId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Active",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "CropCycleId" },
                unique: true,
                filter: "\"Status\" = 1 AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Code",
                table: "ProfitSharingWaterfallSettlements",
                columns: new[] { "OrganizationId", "CropCycleId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitSharingWaterfallParticipantAllocations");

            migrationBuilder.DropTable(
                name: "ProfitSharingWaterfallPriorityAllocations");

            migrationBuilder.DropTable(
                name: "ProfitSharingWaterfallResidualShares");

            migrationBuilder.DropTable(
                name: "ProfitSharingWaterfallSettlements");
        }
    }
}
