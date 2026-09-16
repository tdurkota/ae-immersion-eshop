using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.Catalog.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerIdToCatalogItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "seller_id",
                table: "Catalog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_seller_id",
                table: "Catalog",
                column: "seller_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Catalog_seller_id",
                table: "Catalog");

            migrationBuilder.DropColumn(
                name: "seller_id",
                table: "Catalog");
        }
    }
}
