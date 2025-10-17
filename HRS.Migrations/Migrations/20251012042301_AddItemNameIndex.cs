using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 12, 4, 23, 0, 692, DateTimeKind.Utc).AddTicks(1837), "$2a$11$eUTWAWacQ9IXwHMbu9fm4.VMPVFK6HNjFs2vqA8poZSuHcsY3FoUK", new DateTime(2025, 10, 12, 4, 23, 0, 692, DateTimeKind.Utc).AddTicks(1837) });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Name",
                table: "Items");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 7, 14, 20, 59, 496, DateTimeKind.Utc).AddTicks(5870), "$2a$11$4Uo2uCQbVDlZ0mGygeCZhuQQvYiMUWyTHsu1SN.Nbq3/sGLwUrIUK", new DateTime(2025, 10, 7, 14, 20, 59, 496, DateTimeKind.Utc).AddTicks(5870) });
        }
    }
}
