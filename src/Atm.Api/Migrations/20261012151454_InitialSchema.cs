
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atm.Api.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Core tables for a FRESH database. This migration has already been
            // applied to the development database; these operations are not replayed there.
            migrationBuilder.CreateTable(
                name: "Atms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Atms", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Cassettes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtmMachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    Denomination = table.Column<int>(type: "integer", nullable: true),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cassettes", x => x.Id);
                    table.ForeignKey("FK_Cassettes_Atms_AtmMachineId", x => x.AtmMachineId,
                        "Atms", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepositNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositNotes", x => x.Id);
                    table.ForeignKey("FK_DepositNotes_Cassettes_CassetteId", x => x.CassetteId,
                        "Cassettes", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cassettes_AtmMachineId_Slot",
                table: "Cassettes",
                columns: new[] { "AtmMachineId", "Slot" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositNotes_CassetteId_Currency_Denomination",
                table: "DepositNotes",
                columns: new[] { "CassetteId", "Currency", "Denomination" }, unique: true);

            migrationBuilder.CreateTable(
                name: "Reconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    AtmMachineId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    CreatedBy = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ApprovedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    Status = table.Column<string>(
                        type: "text",
                        nullable: false),

                    TotalDifferenceTRY = table.Column<decimal>(
                        type: "numeric",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Reconciliations",
                        x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    ReconciliationId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    Slot = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    SystemCount = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    PhysicalCount = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Difference = table.Column<int>(
                        type: "integer",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ReconciliationItems",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_ReconciliationItems_Reconciliations_ReconciliationId",
                        column: x => x.ReconciliationId,
                        principalTable: "Reconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_ReconciliationId",
                table: "ReconciliationItems",
                column: "ReconciliationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationItems");

            migrationBuilder.DropTable(
                name: "Reconciliations");

            migrationBuilder.DropTable(name: "DepositNotes");
            migrationBuilder.DropTable(name: "Cassettes");
            migrationBuilder.DropTable(name: "Atms");
        }
    }
}

