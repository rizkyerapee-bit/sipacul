using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivationActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS " +
                "\"IX_CultivationSopSteps_" +
                "OrganizationId_CultivationSopId\";");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CultivationSopSteps_OrganizationId_CultivationSopId_Id",
                table: "CultivationSopSteps",
                columns: new[] { "OrganizationId", "CultivationSopId", "Id" });

            migrationBuilder.CreateTable(
                name: "CultivationActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    CultivationSopId = table.Column<Guid>(type: "uuid", nullable: true),
                    CultivationSopStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    SopStepSequenceSnapshot = table.Column<int>(type: "integer", nullable: true),
                    SopStepNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SopPlannedDayOffsetSnapshot = table.Column<int>(type: "integer", nullable: true),
                    SopEstimatedDurationDaysSnapshot = table.Column<int>(type: "integer", nullable: true),
                    SopIsRequiredSnapshot = table.Column<bool>(type: "boolean", nullable: true),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualCompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SopComplianceStatus = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IssueNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeviationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CultivationActivities", x => x.Id);
                    table.UniqueConstraint("AK_CultivationActivities_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_CultivationActivities_CropCycles_OrganizationId_CropCycleId",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationActivities_CultivationSopSteps_OrganizationId_CultivationSopId_CultivationSopStepId",
                        columns: x => new { x.OrganizationId, x.CultivationSopId, x.CultivationSopStepId },
                        principalTable: "CultivationSopSteps",
                        principalColumns: new[] { "OrganizationId", "CultivationSopId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationActivities_CultivationSops_OrganizationId_CultivationSopId",
                        columns: x => new { x.OrganizationId, x.CultivationSopId },
                        principalTable: "CultivationSops",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationActivities_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CultivationActivityResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultivationActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationActivityResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationActivityResources_CultivationActivities_OrganizationId_CultivationActivityId",
                        columns: x => new { x.OrganizationId, x.CultivationActivityId },
                        principalTable: "CultivationActivities",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CultivationActivityResources_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_IsDeleted",
                table: "CultivationActivities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_OrganizationId_ActivityType",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "ActivityType" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_OrganizationId_CropCycleId_Status",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_OrganizationId_CultivationSopId_Culti~",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "CultivationSopId", "CultivationSopStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_OrganizationId_CultivationSopStepId",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "CultivationSopStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivities_OrganizationId_PlannedDate",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "UX_CultivationActivities_OrganizationId_CropCycleId_Code",
                table: "CultivationActivities",
                columns: new[] { "OrganizationId", "CropCycleId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivityResources_OrganizationId_CultivationActivityId",
                table: "CultivationActivityResources",
                columns: new[] { "OrganizationId", "CultivationActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationActivityResources_OrganizationId_ResourceType",
                table: "CultivationActivityResources",
                columns: new[] { "OrganizationId", "ResourceType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CultivationActivityResources");

            migrationBuilder.DropTable(
                name: "CultivationActivities");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CultivationSopSteps_OrganizationId_CultivationSopId_Id",
                table: "CultivationSopSteps");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSopSteps_OrganizationId_CultivationSopId",
                table: "CultivationSopSteps",
                columns: new[] { "OrganizationId", "CultivationSopId" });
        }
    }
}
