using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHarvestBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HarvestBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GrossQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NetQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUnit = table.Column<int>(type: "integer", nullable: false),
                    QualityGrade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StorageLocation = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
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
                    table.PrimaryKey("PK_HarvestBatches", x => x.Id);
                    table.UniqueConstraint("AK_HarvestBatches_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_HarvestBatches_CropCycles_OrganizationId_CropCycleId",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HarvestBatches_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_IsDeleted",
                table: "HarvestBatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_OrganizationId_CropCycleId_Status",
                table: "HarvestBatches",
                columns: new[] { "OrganizationId", "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_OrganizationId_HarvestDate",
                table: "HarvestBatches",
                columns: new[] { "OrganizationId", "HarvestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_OrganizationId_Status",
                table: "HarvestBatches",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_HarvestBatches_OrganizationId_CropCycleId_Code",
                table: "HarvestBatches",
                columns: new[] { "OrganizationId", "CropCycleId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HarvestBatches");
        }
    }
}
