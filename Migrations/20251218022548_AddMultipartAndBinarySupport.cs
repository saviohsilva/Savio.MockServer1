using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipartAndBinarySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestBodyBase64",
                table: "UnmockedRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyContentType",
                table: "UnmockedRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyFileName",
                table: "UnmockedRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFormJson",
                table: "UnmockedRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyBase64",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyContentType",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyFileName",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFormJson",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyBase64",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyContentType",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyFileName",
                table: "RequestHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyBase64",
                table: "MockEndpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyContentType",
                table: "MockEndpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyFileName",
                table: "MockEndpoints",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestBodyBase64",
                table: "UnmockedRequests");

            migrationBuilder.DropColumn(
                name: "RequestBodyContentType",
                table: "UnmockedRequests");

            migrationBuilder.DropColumn(
                name: "RequestBodyFileName",
                table: "UnmockedRequests");

            migrationBuilder.DropColumn(
                name: "RequestFormJson",
                table: "UnmockedRequests");

            migrationBuilder.DropColumn(
                name: "RequestBodyBase64",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "RequestBodyContentType",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "RequestBodyFileName",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "RequestFormJson",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "ResponseBodyBase64",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "ResponseBodyContentType",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "ResponseBodyFileName",
                table: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "ResponseBodyBase64",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "ResponseBodyContentType",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "ResponseBodyFileName",
                table: "MockEndpoints");
        }
    }
}
