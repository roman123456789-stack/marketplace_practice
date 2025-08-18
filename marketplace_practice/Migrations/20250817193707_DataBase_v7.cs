using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace marketplace_practice.Migrations
{
    /// <inheritdoc />
    public partial class DataBase_v7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_product_id",
                table: "cart_items",
                column: "product_id",
                unique: true);
        }
    }
}
