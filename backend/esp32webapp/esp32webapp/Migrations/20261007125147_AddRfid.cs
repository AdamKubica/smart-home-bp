using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace esp32webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddRfid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Granted = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RfidCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidCards", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RfidCards",
                columns: new[] { "Id", "IsActive", "OwnerName", "Uid" },
                values: new object[,]
                {
                    { 1, true, "Adam", "A1B2C3D4" },
                    { 2, true, "Host", "11223344" },
                    { 3, false, "Stara karta", "DEADBEEF" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidCards_Uid",
                table: "RfidCards",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessLogs");

            migrationBuilder.DropTable(
                name: "RfidCards");
        }
    }
}
