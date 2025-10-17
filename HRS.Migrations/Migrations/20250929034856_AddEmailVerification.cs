using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "EmailVerificationToken", "EmailVerificationTokenExpiry", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 3, 48, 56, 471, DateTimeKind.Utc).AddTicks(1974), null, null, "$2a$11$zMTqtBjPFMYhjjDMLcemS.R5hcqm/PMK/qca6xnUVu8jgRgPEf2ye", new DateTime(2025, 9, 29, 3, 48, 56, 471, DateTimeKind.Utc).AddTicks(1975) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiry",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 26, 4, 29, 39, 202, DateTimeKind.Utc).AddTicks(3800), "$2a$11$ZOKhao/PqZpn1EiJcqnX2.fBU9oCsQbMtd0vaMGqgp8IgZE.Lw/ie", new DateTime(2025, 9, 26, 4, 29, 39, 202, DateTimeKind.Utc).AddTicks(3800) });
        }
    }
}
