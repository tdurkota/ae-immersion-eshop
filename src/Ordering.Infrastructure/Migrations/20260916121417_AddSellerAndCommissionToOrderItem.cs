using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerAndCommissionToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellerId",
                schema: "ordering",
                table: "orderItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                schema: "ordering",
                table: "orderItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0.15m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionRate",
                schema: "ordering",
                table: "orderItems");

            migrationBuilder.DropColumn(
                name: "SellerId",
                schema: "ordering",
                table: "orderItems");
        }
    }
}
