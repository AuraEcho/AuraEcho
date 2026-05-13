using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemovePluginDefaultViewName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manifest_DefaultViewName",
                table: "LocalPlugins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manifest_DefaultViewName",
                table: "LocalPlugins",
                type: "TEXT",
                nullable: true);
        }
    }
}
