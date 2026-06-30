using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teryaq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCashierUserIdForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_CashierUserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CashierUserId",
                table: "Sales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashierUserId",
                table: "Sales",
                column: "CashierUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_CashierUserId",
                table: "Sales",
                column: "CashierUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
