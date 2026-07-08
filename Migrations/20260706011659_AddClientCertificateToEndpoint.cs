using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCertificateToEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireClientCertificate",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiredClientCertificateId",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_RequiredClientCertificateId",
                table: "MockEndpoints",
                column: "RequiredClientCertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_MockEndpoints_MockCertificates_RequiredClientCertificateId",
                table: "MockEndpoints",
                column: "RequiredClientCertificateId",
                principalTable: "MockCertificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MockEndpoints_MockCertificates_RequiredClientCertificateId",
                table: "MockEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_MockEndpoints_RequiredClientCertificateId",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "RequireClientCertificate",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "RequiredClientCertificateId",
                table: "MockEndpoints");
        }
    }
}
