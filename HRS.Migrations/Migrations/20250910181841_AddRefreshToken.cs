using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "RefreshToken", "RefreshTokenExpiry", "UpdatedAt", "UpdatedBy", "UpdatedByUserId" },
                values: new object[] { new DateTime(2025, 9, 10, 18, 18, 40, 881, DateTimeKind.Utc).AddTicks(2270), "$2a$11$VzRmFuyyXsigazkYuA2RMuvFulVyMoIuHfsMkjW/1nAay38Qg0isS", null, null, new DateTime(2025, 9, 10, 18, 18, 40, 881, DateTimeKind.Utc).AddTicks(2270), 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedByUserId",
                table: "Users",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_UpdatedByUserId",
                table: "Users",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_UpdatedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UpdatedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 31, 6, 18, 49, 812, DateTimeKind.Utc).AddTicks(7600), "$2a$11$GT5RNHXUtUYZFUKVH6bT2eAx04bD449/o5wLDkIqwbbkUYbMGdoCa", new DateTime(2025, 8, 31, 6, 18, 49, 812, DateTimeKind.Utc).AddTicks(7600) });
        }
    }
}
