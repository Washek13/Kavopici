using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedBlends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedBlendId",
                table: "CoffeeBlends",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoffeeBlends_LinkedBlendId",
                table: "CoffeeBlends",
                column: "LinkedBlendId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoffeeBlends_CoffeeBlends_LinkedBlendId",
                table: "CoffeeBlends",
                column: "LinkedBlendId",
                principalTable: "CoffeeBlends",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoffeeBlends_CoffeeBlends_LinkedBlendId",
                table: "CoffeeBlends");

            migrationBuilder.DropIndex(
                name: "IX_CoffeeBlends_LinkedBlendId",
                table: "CoffeeBlends");

            migrationBuilder.DropColumn(
                name: "LinkedBlendId",
                table: "CoffeeBlends");
        }
    }
}
