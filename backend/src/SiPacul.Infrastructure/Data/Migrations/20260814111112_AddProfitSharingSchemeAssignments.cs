using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitSharingSchemeAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeFamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SchemeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SchemeDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SchemeVersion = table.Column<int>(type: "integer", nullable: false),
                    ResidualMethod = table.Column<int>(type: "integer", nullable: false),
                    ResidualRecipientCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ProfitSharingSchemeAssignments", x => x.Id);
                    table.UniqueConstraint("AK_ProfitSharingSchemeAssignments_Org_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignments_CropCycle",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignments_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignments_SourceScheme",
                        columns: x => new { x.OrganizationId, x.SourceSchemeId },
                        principalTable: "ProfitSharingSchemes",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeAssignmentParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ParticipantRole = table.Column<int>(type: "integer", nullable: false),
                    ParticipatesInResidualProfit = table.Column<bool>(type: "boolean", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSchemeAssignmentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentParticipants_Assignment",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeAssignmentId },
                        principalTable: "ProfitSharingSchemeAssignments",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentParticipants_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeAssignmentPriorityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    RecipientCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RateNumerator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateDenominator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSchemeAssignmentPriorityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentPriorityRules_Assignment",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeAssignmentId },
                        principalTable: "ProfitSharingSchemeAssignments",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentPriorityRules_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeAssignmentResidualShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RateNumerator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateDenominator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSchemeAssignmentResidualShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentResidualShares_Assignment",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeAssignmentId },
                        principalTable: "ProfitSharingSchemeAssignments",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeAssignmentResidualShares_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentParticipants_Assignment_Code",
                table: "ProfitSharingSchemeAssignmentParticipants",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "ParticipantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentParticipants_Assignment_Sequence",
                table: "ProfitSharingSchemeAssignmentParticipants",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentPriorityRules_Assignment_Code",
                table: "ProfitSharingSchemeAssignmentPriorityRules",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentPriorityRules_Assignment_Sequence",
                table: "ProfitSharingSchemeAssignmentPriorityRules",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentResidualShares_Assignment_Recipient",
                table: "ProfitSharingSchemeAssignmentResidualShares",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "RecipientCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PSAssignmentResidualShares_Assignment_Sequence",
                table: "ProfitSharingSchemeAssignmentResidualShares",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeAssignmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSchemeAssignments_IsDeleted",
                table: "ProfitSharingSchemeAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSchemeAssignments_Org_Scheme",
                table: "ProfitSharingSchemeAssignments",
                columns: new[] { "OrganizationId", "SourceSchemeId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemeAssignments_Org_Cycle",
                table: "ProfitSharingSchemeAssignments",
                columns: new[] { "OrganizationId", "CropCycleId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeAssignmentParticipants");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeAssignmentPriorityRules");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeAssignmentResidualShares");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeAssignments");
        }
    }
}
