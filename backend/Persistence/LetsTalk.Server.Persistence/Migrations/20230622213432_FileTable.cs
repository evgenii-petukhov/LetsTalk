using MySql.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LetsTalk.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filetypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySQL:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filetypes", x => x.Id);
                })
                .Annotation("MySQL:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FileTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_files_filetypes_FileTypeId",
                        column: x => x.FileTypeId,
                        principalTable: "filetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "filetypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Image" },
                    { 2, "Audio" }
                });

            migrationBuilder.InsertData(
                table: "imagecontenttypes",
                columns: new[] { "Id", "Name" },
                values: new object[] { 4, "Webp" });

            migrationBuilder.CreateIndex(
                name: "IX_files_FileTypeId",
                table: "files",
                column: "FileTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "filetypes");

            migrationBuilder.DeleteData(
                table: "imagecontenttypes",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
