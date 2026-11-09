using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace esp32webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NodeId",
                table: "Nodes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Nodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_NodeId",
                table: "Nodes",
                column: "NodeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nodes_NodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Nodes");

            migrationBuilder.AlterColumn<string>(
                name: "NodeId",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
