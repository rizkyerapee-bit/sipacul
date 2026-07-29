using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivationSops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Commodities_OrganizationId_Id",
                table: "Commodities",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateTable(
                name: "CultivationSops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_CultivationSops", x => x.Id);
                    table.UniqueConstraint("AK_CultivationSops_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_CultivationSops_Commodities_OrganizationId_CommodityId",
                        columns: x => new { x.OrganizationId, x.CommodityId },
                        principalTable: "Commodities",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationSops_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CultivationSopSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultivationSopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PlannedDayOffset = table.Column<int>(type: "integer", nullable: false),
                    EstimatedDurationDays = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationSopSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationSopSteps_CultivationSops_OrganizationId_Cultivat~",
                        columns: x => new { x.OrganizationId, x.CultivationSopId },
                        principalTable: "CultivationSops",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CultivationSopSteps_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSops_IsDeleted",
                table: "CultivationSops",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSops_OrganizationId_CommodityId_Status",
                table: "CultivationSops",
                columns: new[] { "OrganizationId", "CommodityId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UX_CultivationSops_OrganizationId_CommodityId_Name",
                table: "CultivationSops",
                columns: new[] { "OrganizationId", "CommodityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSopSteps_OrganizationId_PlannedDayOffset",
                table: "CultivationSopSteps",
                columns: new[] { "OrganizationId", "PlannedDayOffset" });

            migrationBuilder.CreateIndex(
                name: "UX_CultivationSopSteps_OrganizationId_SopId_Sequence",
                table: "CultivationSopSteps",
                columns: new[] { "OrganizationId", "CultivationSopId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CultivationSopSteps");

            migrationBuilder.DropTable(
                name: "CultivationSops");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Commodities_OrganizationId_Id",
                table: "Commodities");
        }
    }
}
