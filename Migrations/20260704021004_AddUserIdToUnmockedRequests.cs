using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToUnmockedRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UnmockedRequests",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnmockedRequests_UserId",
                table: "UnmockedRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnmockedRequests_UserId",
                table: "UnmockedRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UnmockedRequests");
        }
    }
}
