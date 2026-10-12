using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCassetteMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CassetteMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    QuantityChange = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CassetteMovements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CassetteMovements_CassetteId_CreatedAt",
                table: "CassetteMovements",
                columns: new[] { "CassetteId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CassetteMovements");
        }
    }
}
