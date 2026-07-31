using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCropCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LandPlots_OrganizationId_LandId_Id",
                table: "LandPlots",
                columns: new[] { "OrganizationId", "LandId", "Id" });

            migrationBuilder.CreateTable(
                name: "CropCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultivationSopId = table.Column<Guid>(type: "uuid", nullable: true),
                    LandId = table.Column<Guid>(type: "uuid", nullable: false),
                    LandPlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantedArea = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AreaUnit = table.Column<int>(type: "integer", nullable: false),
                    PlannedStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedHarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CropCycles", x => x.Id);
                    table.UniqueConstraint("AK_CropCycles_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_CropCycles_Commodities_OrganizationId_CommodityId",
                        columns: x => new { x.OrganizationId, x.CommodityId },
                        principalTable: "Commodities",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropCycles_CultivationSops_OrganizationId_CultivationSopId",
                        columns: x => new { x.OrganizationId, x.CultivationSopId },
                        principalTable: "CultivationSops",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropCycles_LandPlots_OrganizationId_LandId_LandPlotId",
                        columns: x => new { x.OrganizationId, x.LandId, x.LandPlotId },
                        principalTable: "LandPlots",
                        principalColumns: new[] { "OrganizationId", "LandId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropCycles_Lands_OrganizationId_LandId",
                        columns: x => new { x.OrganizationId, x.LandId },
                        principalTable: "Lands",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropCycles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_IsDeleted",
                table: "CropCycles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_OrganizationId_CommodityId",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "CommodityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_OrganizationId_CultivationSopId",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "CultivationSopId" });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_OrganizationId_LandId_LandPlotId",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "LandId", "LandPlotId" });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_OrganizationId_PlannedDateRange",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "PlannedStartDate", "ExpectedHarvestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_OrganizationId_Status",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_CropCycles_OrganizationId_Code",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CropCycles_OrganizationId_LandId_LandPlotId_InProgress",
                table: "CropCycles",
                columns: new[] { "OrganizationId", "LandId", "LandPlotId" },
                unique: true,
                filter: "\"Status\" = 2 AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropCycles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LandPlots_OrganizationId_LandId_Id",
                table: "LandPlots");
        }
    }
}
