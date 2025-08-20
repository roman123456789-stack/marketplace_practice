using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace marketplace_practice.Migrations
{
    /// <inheritdoc />
    public partial class DataBase_v9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Description", "Name", "NormalizedName" },
                values: new object[] { "Главный администратор системы. Полный доступ.", "MainAdmin", "MAINADMIN" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "Description", "Name", "NormalizedName" },
                values: new object[] { "Администратор системы. Доступ к управлению ресурсами.", "Admin", "ADMIN" });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Description", "Name", "NormalizedName" },
                values: new object[] { 3L, null, "Покупатель. Базовые права на совершение покупок.", "Buyer", "BUYER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Description", "Name", "NormalizedName" },
                values: new object[] { "Администратор системы. Полный доступ.", "Admin", "ADMIN" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "Description", "Name", "NormalizedName" },
                values: new object[] { "Покупатель. Базовые права на совершение покупок.", "Buyer", "BUYER" });
        }
    }
}
