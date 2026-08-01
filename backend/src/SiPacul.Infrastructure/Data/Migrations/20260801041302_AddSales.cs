using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiPacul.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SaleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BuyerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BuyerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentTerm = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.UniqueConstraint("AK_Sales_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Sales_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestBatchCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CropCycleIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommodityIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommodityNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QualityGradeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUnit = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleLines_HarvestBatches_OrganizationId_HarvestBatchId",
                        columns: x => new { x.OrganizationId, x.HarvestBatchId },
                        principalTable: "HarvestBatches",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleLines_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleLines_Sales_OrganizationId_SaleId",
                        columns: x => new { x.OrganizationId, x.SaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_OrganizationId_HarvestBatchId",
                table: "SaleLines",
                columns: new[] { "OrganizationId", "HarvestBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_OrganizationId_SaleId",
                table: "SaleLines",
                columns: new[] { "OrganizationId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "UX_SaleLines_OrganizationId_SaleId_HarvestBatchId",
                table: "SaleLines",
                columns: new[] { "OrganizationId", "SaleId", "HarvestBatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_IsDeleted",
                table: "Sales",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OrganizationId_BuyerName",
                table: "Sales",
                columns: new[] { "OrganizationId", "BuyerName" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OrganizationId_SaleDate",
                table: "Sales",
                columns: new[] { "OrganizationId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OrganizationId_Status",
                table: "Sales",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Sales_OrganizationId_Code",
                table: "Sales",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleLines");

            migrationBuilder.DropTable(
                name: "Sales");
        }
    }
}
