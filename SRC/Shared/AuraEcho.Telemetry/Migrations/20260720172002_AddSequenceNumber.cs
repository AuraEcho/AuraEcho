using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Telemetry.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SequenceNumber",
                table: "TelemetryEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_SessionId_SequenceNumber",
                table: "TelemetryEvents",
                columns: new[] { "SessionId", "SequenceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryEvents_SessionId_SequenceNumber",
                table: "TelemetryEvents");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "TelemetryEvents");
        }
    }
}
