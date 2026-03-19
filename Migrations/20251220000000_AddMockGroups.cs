using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddMockGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockGroups_Name",
                table: "MockGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockGroups_CreatedAt",
                table: "MockGroups",
                column: "CreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "MockGroupId",
                table: "MockEndpoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_MockGroupId",
                table: "MockEndpoints",
                column: "MockGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_MockEndpoints_MockGroups_MockGroupId",
                table: "MockEndpoints",
                column: "MockGroupId",
                principalTable: "MockGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropIndex(
                name: "IX_MockEndpoints_Route_Method",
                table: "MockEndpoints");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_Route_Method",
                table: "MockEndpoints",
                columns: new[] { "Route", "Method" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MockEndpoints_MockGroups_MockGroupId",
                table: "MockEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_MockEndpoints_MockGroupId",
                table: "MockEndpoints");

            migrationBuilder.DropColumn(
                name: "MockGroupId",
                table: "MockEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_MockEndpoints_Route_Method",
                table: "MockEndpoints");

            migrationBuilder.CreateIndex(
                name: "IX_MockEndpoints_Route_Method",
                table: "MockEndpoints",
                columns: new[] { "Route", "Method" },
                unique: true);

            migrationBuilder.DropTable(
                name: "MockGroups");
        }
    }
}
