using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockEndpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Route = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    HeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseBodyJson = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseBodyRaw = table.Column<string>(type: "TEXT", nullable: true),
                    DelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CallCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCalledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockEndpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MockEndpointId = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RequestHeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseHeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    DelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClientIp = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestHistory_MockEndpoints_MockEndpointId",
                        column: x => x.MockEndpointId,
                        principalTable: "MockEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_CreatedAt",
                table: "MockEndpoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_IsActive",
                table: "MockEndpoints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_Route_Method",
                table: "MockEndpoints",
                columns: new[] { "Route", "Method" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_MockEndpointId",
                table: "RequestHistory",
                column: "MockEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_RequestedAt",
                table: "RequestHistory",
                column: "RequestedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestHistory");

            migrationBuilder.DropTable(
                name: "MockEndpoints");
        }
    }
}
