using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManifestDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manifest_Description",
                table: "LocalPlugins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manifest_Description",
                table: "LocalPlugins",
                type: "TEXT",
                nullable: true);
        }
    }
}
