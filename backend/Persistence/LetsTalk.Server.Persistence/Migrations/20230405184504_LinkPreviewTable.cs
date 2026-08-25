using MySql.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetsTalk.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkPreviewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkPreviewId",
                table: "messages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LinkPreview",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Url = table.Column<string>(type: "varchar(3000)", maxLength: 3000, nullable: true)
                        .Annotation("MySQL:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySQL:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySQL:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkPreview", x => x.Id);
                })
                .Annotation("MySQL:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_messages_LinkPreviewId",
                table: "messages",
                column: "LinkPreviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_LinkPreview_LinkPreviewId",
                table: "messages",
                column: "LinkPreviewId",
                principalTable: "LinkPreview",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_LinkPreview_LinkPreviewId",
                table: "messages");

            migrationBuilder.DropTable(
                name: "LinkPreview");

            migrationBuilder.DropIndex(
                name: "IX_messages_LinkPreviewId",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "LinkPreviewId",
                table: "messages");
        }
    }
}
