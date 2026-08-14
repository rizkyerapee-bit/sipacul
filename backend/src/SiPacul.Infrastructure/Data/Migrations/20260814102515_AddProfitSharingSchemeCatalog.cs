using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitSharingSchemeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeFamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResidualMethod = table.Column<int>(type: "integer", nullable: false),
                    ResidualRecipientCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ProfitSharingSchemes", x => x.Id);
                    table.UniqueConstraint("AK_ProfitSharingSchemes_Org_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemes_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ParticipantRole = table.Column<int>(type: "integer", nullable: false),
                    ParticipatesInResidualProfit = table.Column<bool>(type: "boolean", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSchemeParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeParticipants_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeParticipants_Scheme",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeId },
                        principalTable: "ProfitSharingSchemes",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemePriorityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProfitSharingSchemePriorityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemePriorityRules_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemePriorityRules_Scheme",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeId },
                        principalTable: "ProfitSharingSchemes",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharingSchemeResidualShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitSharingSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RateNumerator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateDenominator = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharingSchemeResidualShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeResidualShares_Organization",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitSharingSchemeResidualShares_Scheme",
                        columns: x => new { x.OrganizationId, x.ProfitSharingSchemeId },
                        principalTable: "ProfitSharingSchemes",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemeParticipants_Scheme_Code",
                table: "ProfitSharingSchemeParticipants",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "ParticipantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemeParticipants_Scheme_Sequence",
                table: "ProfitSharingSchemeParticipants",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemePriorityRules_Scheme_Code",
                table: "ProfitSharingSchemePriorityRules",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemePriorityRules_Scheme_Sequence",
                table: "ProfitSharingSchemePriorityRules",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemeResidualShares_Scheme_Recipient",
                table: "ProfitSharingSchemeResidualShares",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "RecipientCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemeResidualShares_Scheme_Sequence",
                table: "ProfitSharingSchemeResidualShares",
                columns: new[] { "OrganizationId", "ProfitSharingSchemeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSchemes_IsDeleted",
                table: "ProfitSharingSchemes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitSharingSchemes_Org_Status",
                table: "ProfitSharingSchemes",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemes_Org_Code_Version",
                table: "ProfitSharingSchemes",
                columns: new[] { "OrganizationId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemes_Org_Family_OpenStatus",
                table: "ProfitSharingSchemes",
                columns: new[] { "OrganizationId", "SchemeFamilyId", "Status" },
                unique: true,
                filter: "\"Status\" IN (1, 2) AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "UX_ProfitSharingSchemes_Org_Family_Version",
                table: "ProfitSharingSchemes",
                columns: new[] { "OrganizationId", "SchemeFamilyId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeParticipants");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemePriorityRules");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemeResidualShares");

            migrationBuilder.DropTable(
                name: "ProfitSharingSchemes");
        }
    }
}
