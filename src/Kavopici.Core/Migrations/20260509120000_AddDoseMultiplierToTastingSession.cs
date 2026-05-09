using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddDoseMultiplierToTastingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DoseMultiplier",
                table: "TastingSessions",
                type: "decimal(3,1)",
                nullable: false,
                defaultValue: 1.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoseMultiplier",
                table: "TastingSessions");
        }
    }
}
