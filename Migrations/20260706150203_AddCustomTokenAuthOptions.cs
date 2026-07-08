using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomTokenAuthOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomTokenPrefix",
                table: "MockAuthConfigs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomTokenReturnLocation",
                table: "MockAuthConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomTokenReturnName",
                table: "MockAuthConfigs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomTokenSuffix",
                table: "MockAuthConfigs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomValidationParamsJson",
                table: "MockAuthConfigs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomTokenPrefix",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "CustomTokenReturnLocation",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "CustomTokenReturnName",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "CustomTokenSuffix",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "CustomValidationParamsJson",
                table: "MockAuthConfigs");
        }
    }
}
