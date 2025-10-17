using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RentalOrderAndItemMaintenanceAndPaymentDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_ReturnedById",
                        column: x => x.ReturnedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemMaintenances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    RentalOrderId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    QuantityFixed = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemMaintenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemMaintenances_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemMaintenances_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemMaintenances_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemMaintenances_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RentalOrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StripeSessionId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StripePaymentIntentId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_RentalOrderItems_ItemRates_ItemRateId",
                        column: x => x.ItemRateId,
                        principalTable: "ItemRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RentalOrderItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                        name: "FK_RentalOrderPackages_PackageRates_PackageRateId",
                        column: x => x.PackageRateId,
                        principalTable: "PackageRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RentalOrderPackages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                        name: "FK_RentalOrderPackageItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RentalOrderPackageItems_RentalOrderPackages_RentalOrderPacka~",
                        column: x => x.RentalOrderPackageId,
                        principalTable: "RentalOrderPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 15, 6, 9, 39, 423, DateTimeKind.Utc).AddTicks(770), "$2a$11$vCt0yqV9Ch71ywbiJKUFpOoREGNnTQpYE0gI5777E/HxQBzszZmIu", new DateTime(2025, 10, 15, 6, 9, 39, 423, DateTimeKind.Utc).AddTicks(770) });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaintenances_CreatedById",
                table: "ItemMaintenances",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaintenances_ItemId",
                table: "ItemMaintenances",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaintenances_RentalOrderId",
                table: "ItemMaintenances",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMaintenances_UpdatedById",
                table: "ItemMaintenances",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedById",
                table: "Payments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RentalOrderId",
                table: "Payments",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UpdatedById",
                table: "Payments",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_ItemId",
                table: "RentalOrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_ItemRateId",
                table: "RentalOrderItems",
                column: "ItemRateId");

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
                name: "IX_RentalOrderPackages_PackageRateId",
                table: "RentalOrderPackages",
                column: "PackageRateId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPackages_RentalOrderId",
                table: "RentalOrderPackages",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_ApprovedById",
                table: "RentalOrders",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_Channel",
                table: "RentalOrders",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_ClosedById",
                table: "RentalOrders",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_CreatedById",
                table: "RentalOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_CustomerId",
                table: "RentalOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_ReturnedById",
                table: "RentalOrders",
                column: "ReturnedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_Status",
                table: "RentalOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_StripeSessionId",
                table: "RentalOrders",
                column: "StripeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_UpdatedById",
                table: "RentalOrders",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemMaintenances");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RentalOrderItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPackageItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPackages");

            migrationBuilder.DropTable(
                name: "RentalOrders");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 7, 14, 20, 59, 496, DateTimeKind.Utc).AddTicks(5870), "$2a$11$4Uo2uCQbVDlZ0mGygeCZhuQQvYiMUWyTHsu1SN.Nbq3/sGLwUrIUK", new DateTime(2025, 10, 7, 14, 20, 59, 496, DateTimeKind.Utc).AddTicks(5870) });
        }
    }
}
