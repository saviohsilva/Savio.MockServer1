using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUnmockedRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnmockedRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RequestHeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastClientIp = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MockCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    MockCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmockedRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnmockedRequests_LastSeenAt",
                table: "UnmockedRequests",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_UnmockedRequests_MockCreated",
                table: "UnmockedRequests",
                column: "MockCreated");

            migrationBuilder.CreateIndex(
                name: "IX_UnmockedRequests_Route_Method",
                table: "UnmockedRequests",
                columns: new[] { "Route", "Method" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnmockedRequests");
        }
    }
}
