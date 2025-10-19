using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddrentalOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    GuestName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuestPhone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuestEmail = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReturnedById = table.Column<int>(type: "int", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedById = table.Column<int>(type: "int", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HasIssues = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ItemsGoodCount = table.Column<int>(type: "int", nullable: false),
                    ItemsIssueCount = table.Column<int>(type: "int", nullable: false),
                    ReturnRemarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StripeSessionId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrders", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RentalOrderId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemRateId = table.Column<int>(type: "int", nullable: true),
                    ItemNameSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DailyRateSnapshot = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    GoodQty = table.Column<int>(type: "int", nullable: false),
                    RepairQty = table.Column<int>(type: "int", nullable: false),
                    DamagedQty = table.Column<int>(type: "int", nullable: false),
                    LostQty = table.Column<int>(type: "int", nullable: false),
                    ConditionRemarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderItems_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RentalOrderId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: true),
                    PackageRateId = table.Column<int>(type: "int", nullable: true),
                    PackageNameSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DailyRateSnapshot = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderPackages_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderPackageItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RentalOrderPackageId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemNameSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuantityPerPackageSnapshot = table.Column<int>(type: "int", nullable: false),
                    GoodQty = table.Column<int>(type: "int", nullable: false),
                    RepairQty = table.Column<int>(type: "int", nullable: false),
                    DamagedQty = table.Column<int>(type: "int", nullable: false),
                    LostQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderPackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderPackageItems_RentalOrderPackages_RentalOrderPacka~",
                        column: x => x.RentalOrderPackageId,
                        principalTable: "RentalOrderPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_ItemId",
                table: "RentalOrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_RentalOrderId",
                table: "RentalOrderItems",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPackageItems_ItemId",
                table: "RentalOrderPackageItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPackageItems_RentalOrderPackageId",
                table: "RentalOrderPackageItems",
                column: "RentalOrderPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPackages_PackageId",
                table: "RentalOrderPackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPackages_RentalOrderId",
                table: "RentalOrderPackages",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_Channel",
                table: "RentalOrders",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_Status",
                table: "RentalOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_StripeSessionId",
                table: "RentalOrders",
                column: "StripeSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalOrderItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPackageItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPackages");

            migrationBuilder.DropTable(
                name: "RentalOrders");
        }
    }
}
