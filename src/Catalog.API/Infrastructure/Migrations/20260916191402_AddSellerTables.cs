using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OldPrice",
                table: "Catalog",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "Catalog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SellerPayouts",
                columns: table => new
                {
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineItemId = table.Column<int>(type: "integer", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellerAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPayouts", x => x.PayoutId);
                });

            migrationBuilder.CreateTable(
                name: "Sellers",
                columns: table => new
                {
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankAccountInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sellers", x => x.SellerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_SellerId",
                table: "Catalog",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_OrderId",
                table: "SellerPayouts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_SellerId_CreatedAt",
                table: "SellerPayouts",
                columns: new[] { "SellerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayouts_Status",
                table: "SellerPayouts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_Email",
                table: "Sellers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_Status",
                table: "Sellers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_Status_CreatedAt",
                table: "Sellers",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Catalog_Sellers_SellerId",
                table: "Catalog",
                column: "SellerId",
                principalTable: "Sellers",
                principalColumn: "SellerId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Catalog_Sellers_SellerId",
                table: "Catalog");

            migrationBuilder.DropTable(
                name: "SellerPayouts");

            migrationBuilder.DropTable(
                name: "Sellers");

            migrationBuilder.DropIndex(
                name: "IX_Catalog_SellerId",
                table: "Catalog");

            migrationBuilder.DropColumn(
                name: "OldPrice",
                table: "Catalog");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Catalog");
        }
    }
}
