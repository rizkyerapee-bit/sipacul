using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations;

public partial class MakeCultivationSopStepOrderDeferrable :
    Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name:
                "UX_CultivationSopSteps_" +
                "OrganizationId_SopId_Sequence",
            table: "CultivationSopSteps");

        migrationBuilder.Sql(
            """
            ALTER TABLE "CultivationSopSteps"
            ADD CONSTRAINT
                "UQ_CultivationSopSteps_OrganizationId_SopId_Sequence"
            UNIQUE (
                "OrganizationId",
                "CultivationSopId",
                "Sequence"
            )
            DEFERRABLE INITIALLY DEFERRED;
            """);
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "CultivationSopSteps"
            DROP CONSTRAINT
                "UQ_CultivationSopSteps_OrganizationId_SopId_Sequence";
            """);

        migrationBuilder.CreateIndex(
            name:
                "UX_CultivationSopSteps_" +
                "OrganizationId_SopId_Sequence",
            table: "CultivationSopSteps",
            columns:
                new[]
                {
                    "OrganizationId",
                    "CultivationSopId",
                    "Sequence"
                },
            unique: true);
    }
}
