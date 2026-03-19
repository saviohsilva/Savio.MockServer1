using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddMockBinaryBlobStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponseBinaryBlobId",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MockBinaryBlobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Bytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockBinaryBlobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockBinaryBlobs_CreatedAt",
                table: "MockBinaryBlobs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockBinaryBlobs");

            migrationBuilder.DropColumn(
                name: "ResponseBinaryBlobId",
                table: "MockEndpoints");
        }
    }
}
