using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class AddTastingNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TastingNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TastingNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoffeeBlends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Roaster = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RoastLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoffeeBlends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoffeeBlends_Users_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TastingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlendId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TastingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TastingSessions_CoffeeBlends_BlendId",
                        column: x => x.BlendId,
                        principalTable: "CoffeeBlends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlendId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.CheckConstraint("CK_Rating_Stars", "[Stars] >= 1 AND [Stars] <= 5");
                    table.ForeignKey(
                        name: "FK_Ratings_CoffeeBlends_BlendId",
                        column: x => x.BlendId,
                        principalTable: "CoffeeBlends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ratings_TastingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TastingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingTastingNotes",
                columns: table => new
                {
                    RatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    TastingNoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingTastingNotes", x => new { x.RatingId, x.TastingNoteId });
                    table.ForeignKey(
                        name: "FK_RatingTastingNotes_Ratings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "Ratings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RatingTastingNotes_TastingNotes_TastingNoteId",
                        column: x => x.TastingNoteId,
                        principalTable: "TastingNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TastingNotes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Ovocná" },
                    { 2, "Ořechová" },
                    { 3, "Čokoládová" },
                    { 4, "Karamelová" },
                    { 5, "Květinová" },
                    { 6, "Kořeněná" },
                    { 7, "Citrusová" },
                    { 8, "Medová" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoffeeBlends_SupplierId",
                table: "CoffeeBlends",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_BlendId",
                table: "Ratings",
                column: "BlendId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_SessionId",
                table: "Ratings",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_SessionId",
                table: "Ratings",
                columns: new[] { "UserId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingTastingNotes_TastingNoteId",
                table: "RatingTastingNotes",
                column: "TastingNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_TastingNotes_Name",
                table: "TastingNotes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TastingSessions_BlendId",
                table: "TastingSessions",
                column: "BlendId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RatingTastingNotes");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "TastingNotes");

            migrationBuilder.DropTable(
                name: "TastingSessions");

            migrationBuilder.DropTable(
                name: "CoffeeBlends");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
