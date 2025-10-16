using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoBackend.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdateImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profilepicture",
                table: "user");

            migrationBuilder.AddColumn<string>(
                name: "profilepictureurl",
                table: "user",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profilepictureurl",
                table: "user");

            migrationBuilder.AddColumn<byte[]>(
                name: "profilepicture",
                table: "user",
                type: "bytea",
                nullable: true);
        }
    }
}
