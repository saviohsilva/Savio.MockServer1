using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificatesAndAuthConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthConfigId",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthEndpointRole",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MockCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Thumbprint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CertificateData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    HasPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockCertificates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAuthConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GenerateJwtToken = table.Column<bool>(type: "INTEGER", nullable: false),
                    JwtSecretKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    JwtExpirationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    JwtIssuer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    JwtAudience = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    JwtAdditionalClaimsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ApiKeyHeader = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApiKeyValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequireCertificate = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredCertificateId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAuthConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockAuthConfigs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MockAuthConfigs_MockCertificates_RequiredCertificateId",
                        column: x => x.RequiredCertificateId,
                        principalTable: "MockCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_AuthConfigId",
                table: "MockEndpoints",
                column: "AuthConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_MockAuthConfigs_CreatedAt",
                table: "MockAuthConfigs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockAuthConfigs_RequiredCertificateId",
                table: "MockAuthConfigs",
                column: "RequiredCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_MockAuthConfigs_UserId",
                table: "MockAuthConfigs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MockCertificates_CreatedAt",
                table: "MockCertificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockCertificates_Thumbprint",
                table: "MockCertificates",
                column: "Thumbprint");

            migrationBuilder.CreateIndex(
                name: "IX_MockCertificates_UserId",
                table: "MockCertificates",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MockEndpoints_MockAuthConfigs_AuthConfigId",
                table: "MockEndpoints",
                column: "AuthConfigId",
                principalTable: "MockAuthConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MockEndpoints_MockAuthConfigs_AuthConfigId",
                table: "MockEndpoints");

            migrationBuilder.DropTable(
                name: "MockAuthConfigs");

            migrationBuilder.DropTable(
                name: "MockCertificates");

            migrationBuilder.DropIndex(
                name: "IX_MockEndpoints_AuthConfigId",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "AuthConfigId",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "AuthEndpointRole",
                table: "MockEndpoints");
        }
    }
}
