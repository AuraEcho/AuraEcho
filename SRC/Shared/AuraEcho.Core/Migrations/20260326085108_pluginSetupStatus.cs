using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations
{
    /// <inheritdoc />
    public partial class pluginSetupStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSetup",
                table: "LocalPlugins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSetup",
                table: "LocalPlugins");
        }
    }
}
