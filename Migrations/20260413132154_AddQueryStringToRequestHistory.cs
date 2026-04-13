using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryStringToRequestHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QueryString",
                table: "RequestHistory",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QueryString",
                table: "RequestHistory");
        }
    }
}
