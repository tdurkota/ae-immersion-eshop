using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.Sellers.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seller_payouts",
                columns: table => new
                {
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    order_line_item_id = table.Column<int>(type: "integer", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    seller_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_payouts", x => x.payout_id);
                    table.CheckConstraint("CK_seller_payouts_amounts_non_negative", "gross_amount >= 0 AND commission_amount >= 0 AND seller_amount >= 0");
                    table.CheckConstraint("CK_seller_payouts_amounts_reconcile", "gross_amount = commission_amount + seller_amount");
                    table.CheckConstraint("CK_seller_payouts_status_valid", "status IN ('Pending', 'Processed', 'Paid')");
                    table.ForeignKey(
                        name: "FK_seller_payouts_sellers_seller_id",
                        column: x => x.seller_id,
                        principalTable: "sellers",
                        principalColumn: "seller_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seller_payouts_order_id_order_line_item_id",
                table: "seller_payouts",
                columns: new[] { "order_id", "order_line_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_payouts_seller_id_created_at",
                table: "seller_payouts",
                columns: new[] { "seller_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seller_payouts");
        }
    }
}
