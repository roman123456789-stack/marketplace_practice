using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace marketplace_practice.Migrations
{
    /// <inheritdoc />
    public partial class DataBase_v8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "product_category_groups");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "subcategories");

            migrationBuilder.DropIndex(
                name: "IX_order_items_product_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "cart_items");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "order_items",
                newName: "cart_item_id");

            migrationBuilder.AddColumn<decimal>(
                name: "promotional_price",
                table: "products",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "size",
                table: "products",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stock_quantity",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "parent_category_id",
                table: "categories",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    category_id = table.Column<short>(type: "smallint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => new { x.category_id, x.product_id });
                    table.ForeignKey(
                        name: "FK_product_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_categories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_cart_item_id",
                table: "order_items",
                column: "cart_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category_id",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_product_id",
                table: "product_categories",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_categories_parent_category_id",
                table: "categories",
                column: "parent_category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_cart_items_cart_item_id",
                table: "order_items",
                column: "cart_item_id",
                principalTable: "cart_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_categories_parent_category_id",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_cart_items_cart_item_id",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropIndex(
                name: "IX_order_items_cart_item_id",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_categories_parent_category_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "promotional_price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "size",
                table: "products");

            migrationBuilder.DropColumn(
                name: "stock_quantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "parent_category_id",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "cart_item_id",
                table: "order_items",
                newName: "product_id");

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "cart_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "subcategories",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subcategories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<short>(type: "smallint", nullable: false),
                    subcategory_id = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_groups_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_groups_subcategories_subcategory_id",
                        column: x => x.subcategory_id,
                        principalTable: "subcategories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_category_groups",
                columns: table => new
                {
                    group_id = table.Column<short>(type: "smallint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_category_groups", x => new { x.group_id, x.product_id });
                    table.ForeignKey(
                        name: "FK_product_category_groups_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_category_groups_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_category_id",
                table: "groups",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_subcategory_id",
                table: "groups",
                column: "subcategory_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_category_groups_product_id",
                table: "product_category_groups",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
