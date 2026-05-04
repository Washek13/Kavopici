using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddAppConfigAndTastingNoteTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the existing unique index on TastingNote.Name so we can replace it
            //    with a composite (Theme, Name) index.
            migrationBuilder.DropIndex(
                name: "IX_TastingNotes_Name",
                table: "TastingNotes");

            // 2. Add the Theme column to TastingNotes with default "Coffee" so existing
            //    rows in legacy databases backfill correctly.
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "TastingNotes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Coffee");

            // 3. Update the seeded coffee tasting notes to set their Theme value (these
            //    are the rows seeded via HasData in earlier migrations).
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Theme",
                value: "Coffee");
            migrationBuilder.UpdateData(
                table: "TastingNotes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Theme",
                value: "Coffee");

            // 4. Recreate the unique index on the composite (Theme, Name).
            migrationBuilder.CreateIndex(
                name: "IX_TastingNotes_Theme_Name",
                table: "TastingNotes",
                columns: new[] { "Theme", "Name" },
                unique: true);

            // 5. Create the AppConfigs table. The row is inserted at runtime by
            //    AppConfigService — never seeded — because the choice depends on user input.
            migrationBuilder.CreateTable(
                name: "AppConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppConfigs");

            migrationBuilder.DropIndex(
                name: "IX_TastingNotes_Theme_Name",
                table: "TastingNotes");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "TastingNotes");

            migrationBuilder.CreateIndex(
                name: "IX_TastingNotes_Name",
                table: "TastingNotes",
                column: "Name",
                unique: true);
        }
    }
}
