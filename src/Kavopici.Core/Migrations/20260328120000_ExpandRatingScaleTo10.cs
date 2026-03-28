using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavopici.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRatingScaleTo10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite doesn't support ALTER TABLE DROP CONSTRAINT, so we rebuild the table
            // with the new CHECK constraint and double the star values during the copy.
            migrationBuilder.Sql("PRAGMA foreign_keys=off;");

            migrationBuilder.Sql("""
                CREATE TABLE "Ratings_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Ratings" PRIMARY KEY AUTOINCREMENT,
                    "BlendId" INTEGER NOT NULL,
                    "UserId" INTEGER NOT NULL,
                    "SessionId" INTEGER NOT NULL,
                    "Stars" INTEGER NOT NULL,
                    "Comment" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "CK_Rating_Stars" CHECK ([Stars] >= 1 AND [Stars] <= 10),
                    CONSTRAINT "FK_Ratings_CoffeeBlends_BlendId" FOREIGN KEY ("BlendId") REFERENCES "CoffeeBlends" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Ratings_TastingSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "TastingSessions" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Ratings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Ratings_new" ("Id", "BlendId", "UserId", "SessionId", "Stars", "Comment", "CreatedAt")
                SELECT "Id", "BlendId", "UserId", "SessionId", "Stars" * 2, "Comment", "CreatedAt" FROM "Ratings";
                """);

            migrationBuilder.Sql("""DROP TABLE "Ratings";""");
            migrationBuilder.Sql("""ALTER TABLE "Ratings_new" RENAME TO "Ratings";""");

            migrationBuilder.Sql("""CREATE INDEX "IX_Ratings_BlendId" ON "Ratings" ("BlendId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_Ratings_SessionId" ON "Ratings" ("SessionId");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Ratings_UserId_SessionId" ON "Ratings" ("UserId", "SessionId");""");

            migrationBuilder.Sql("PRAGMA foreign_keys=on;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys=off;");

            migrationBuilder.Sql("""
                CREATE TABLE "Ratings_old" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Ratings" PRIMARY KEY AUTOINCREMENT,
                    "BlendId" INTEGER NOT NULL,
                    "UserId" INTEGER NOT NULL,
                    "SessionId" INTEGER NOT NULL,
                    "Stars" INTEGER NOT NULL,
                    "Comment" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "CK_Rating_Stars" CHECK ([Stars] >= 1 AND [Stars] <= 5),
                    CONSTRAINT "FK_Ratings_CoffeeBlends_BlendId" FOREIGN KEY ("BlendId") REFERENCES "CoffeeBlends" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Ratings_TastingSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "TastingSessions" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Ratings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Ratings_old" ("Id", "BlendId", "UserId", "SessionId", "Stars", "Comment", "CreatedAt")
                SELECT "Id", "BlendId", "UserId", "SessionId", "Stars" / 2, "Comment", "CreatedAt" FROM "Ratings";
                """);

            migrationBuilder.Sql("""DROP TABLE "Ratings";""");
            migrationBuilder.Sql("""ALTER TABLE "Ratings_old" RENAME TO "Ratings";""");

            migrationBuilder.Sql("""CREATE INDEX "IX_Ratings_BlendId" ON "Ratings" ("BlendId");""");
            migrationBuilder.Sql("""CREATE INDEX "IX_Ratings_SessionId" ON "Ratings" ("SessionId");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Ratings_UserId_SessionId" ON "Ratings" ("UserId", "SessionId");""");

            migrationBuilder.Sql("PRAGMA foreign_keys=on;");
        }
    }
}
