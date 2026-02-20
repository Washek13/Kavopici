using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddBlendPriceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightGrams",
                table: "CoffeeBlends",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceCzk",
                table: "CoffeeBlends",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKg",
                table: "CoffeeBlends",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightGrams",
                table: "CoffeeBlends");

            migrationBuilder.DropColumn(
                name: "PriceCzk",
                table: "CoffeeBlends");

            migrationBuilder.DropColumn(
                name: "PricePerKg",
                table: "CoffeeBlends");
        }
    }
}
