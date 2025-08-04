using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace marketplace_practice.Migrations
{
    /// <inheritdoc />
    public partial class DataBase_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "users",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "is_verified",
                table: "users",
                newName: "IsVerified");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "users",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "roles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "roles",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "roles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "roles",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "roles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "roles");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "users",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "users",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "IsVerified",
                table: "users",
                newName: "is_verified");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "users",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "users",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "roles",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "roles",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "roles",
                newName: "created_at");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "id",
                table: "roles",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");
        }
    }
}
