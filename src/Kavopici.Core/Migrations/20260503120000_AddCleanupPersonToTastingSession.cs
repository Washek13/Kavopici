using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupPersonToTastingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CleanupPersonId",
                table: "TastingSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CleanupCompleted",
                table: "TastingSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TastingSessions_CleanupPersonId",
                table: "TastingSessions",
                column: "CleanupPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_TastingSessions_Users_CleanupPersonId",
                table: "TastingSessions",
                column: "CleanupPersonId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TastingSessions_Users_CleanupPersonId",
                table: "TastingSessions");

            migrationBuilder.DropIndex(
                name: "IX_TastingSessions_CleanupPersonId",
                table: "TastingSessions");

            migrationBuilder.DropColumn(
                name: "CleanupPersonId",
                table: "TastingSessions");

            migrationBuilder.DropColumn(
                name: "CleanupCompleted",
                table: "TastingSessions");
        }
    }
}
