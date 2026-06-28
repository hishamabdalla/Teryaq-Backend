using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teryaq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReorderLevelToStockBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "StockBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_BranchId_QuantityOnHand",
                table: "StockBatches",
                columns: new[] { "BranchId", "QuantityOnHand" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockBatches_BranchId_QuantityOnHand",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "StockBatches");
        }
    }
}
