using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthCredentialCustomParamMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PasswordParamLocation",
                table: "MockAuthConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PasswordParamName",
                table: "MockAuthConfigs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsernameParamLocation",
                table: "MockAuthConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UsernameParamName",
                table: "MockAuthConfigs",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordParamLocation",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "PasswordParamName",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "UsernameParamLocation",
                table: "MockAuthConfigs");

            migrationBuilder.DropColumn(
                name: "UsernameParamName",
                table: "MockAuthConfigs");
        }
    }
}
