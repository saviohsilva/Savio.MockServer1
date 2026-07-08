using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savio.MockServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestHistoryCompositeIndex : Migration
    {
        private static readonly string[] IndexColumns = ["MockEndpointId", "RequestedAt"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_MockEndpointId_RequestedAt",
                table: "RequestHistory",
                columns: IndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestHistory_MockEndpointId_RequestedAt",
                table: "RequestHistory");
        }
    }
}
