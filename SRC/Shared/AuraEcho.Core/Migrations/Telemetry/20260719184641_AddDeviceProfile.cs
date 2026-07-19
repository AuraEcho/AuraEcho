using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations.Telemetry
{
    /// <inheritdoc />
    public partial class AddDeviceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CpuCoreCount",
                table: "TelemetryEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CpuModel",
                table: "TelemetryEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GpuModel",
                table: "TelemetryEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ScreenDpi",
                table: "TelemetryEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScreenResolution",
                table: "TelemetryEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuCoreCount",
                table: "TelemetryEvents");

            migrationBuilder.DropColumn(
                name: "CpuModel",
                table: "TelemetryEvents");

            migrationBuilder.DropColumn(
                name: "GpuModel",
                table: "TelemetryEvents");

            migrationBuilder.DropColumn(
                name: "ScreenDpi",
                table: "TelemetryEvents");

            migrationBuilder.DropColumn(
                name: "ScreenResolution",
                table: "TelemetryEvents");
        }
    }
}
