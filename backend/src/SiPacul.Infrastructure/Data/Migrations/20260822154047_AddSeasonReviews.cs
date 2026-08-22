using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeasonReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LessonsLearned = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    NextSeasonRecommendations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SeasonReviews", x => x.Id);
                    table.UniqueConstraint("AK_SeasonReviews_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_SeasonReviews_CropCycles_OrganizationId_CropCycleId",
                        columns: x => new { x.OrganizationId, x.CropCycleId },
                        principalTable: "CropCycles",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonReviews_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonReviews_IsDeleted",
                table: "SeasonReviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonReviews_OrganizationId_Status_ReviewDate",
                table: "SeasonReviews",
                columns: new[] { "OrganizationId", "Status", "ReviewDate" });

            migrationBuilder.CreateIndex(
                name: "UX_SeasonReviews_OrganizationId_CropCycleId_Active",
                table: "SeasonReviews",
                columns: new[] { "OrganizationId", "CropCycleId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeasonReviews");
        }
    }
}
