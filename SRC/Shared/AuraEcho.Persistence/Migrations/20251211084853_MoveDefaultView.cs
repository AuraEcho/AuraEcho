using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations.Host
{
    /// <inheritdoc />
    public partial class MoveDefaultView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultView",
                table: "PluginRegistries");

            migrationBuilder.AddColumn<string>(
                name: "Manifest_DefaultViewName",
                table: "PluginRegistries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manifest_DefaultViewName",
                table: "PluginRegistries");

            migrationBuilder.AddColumn<string>(
                name: "DefaultView",
                table: "PluginRegistries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
