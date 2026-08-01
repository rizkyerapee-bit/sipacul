using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCapitalContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapitalContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContributionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContributorCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContributorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContributorRole = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CapitalContributions", x => x.Id);
                    table.UniqueConstraint("AK_CapitalContributions_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_CapitalContributions_CropCycles_OrganizationId_CropCycleId",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CapitalContributions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalContributions_IsDeleted",
                table: "CapitalContributions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalContributions_OrganizationId_ContributionDate",
                table: "CapitalContributions",
                columns: new[] { "OrganizationId", "ContributionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalContributions_OrganizationId_CropCycleId_ContributorCode",
                table: "CapitalContributions",
                columns: new[] { "OrganizationId", "CropCycleId", "ContributorCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalContributions_OrganizationId_CropCycleId_ContributorRole_Status",
                table: "CapitalContributions",
                columns: new[] { "OrganizationId", "CropCycleId", "ContributorRole", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalContributions_OrganizationId_CropCycleId_Status",
                table: "CapitalContributions",
                columns: new[] { "OrganizationId", "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_CapitalContributions_OrganizationId_CropCycleId_Code",
                table: "CapitalContributions",
                columns: new[] { "OrganizationId", "CropCycleId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapitalContributions");
        }
    }
}
