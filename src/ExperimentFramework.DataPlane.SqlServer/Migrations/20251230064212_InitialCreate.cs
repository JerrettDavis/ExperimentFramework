using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExperimentFramework.DataPlane.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "ExperimentEvents",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentEvents_CorrelationId",
                schema: "dbo",
                table: "ExperimentEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentEvents_CreatedAt",
                schema: "dbo",
                table: "ExperimentEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentEvents_EventId",
                schema: "dbo",
                table: "ExperimentEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentEvents_EventType",
                schema: "dbo",
                table: "ExperimentEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentEvents_Timestamp",
                schema: "dbo",
                table: "ExperimentEvents",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperimentEvents",
                schema: "dbo");
        }
    }
}
