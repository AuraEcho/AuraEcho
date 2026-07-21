using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations.Host
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PluginRegistries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Manifest_Id = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Author = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_PluginName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Version = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Description = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_EntryAssemblyName = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultView = table.Column<string>(type: "TEXT", nullable: false),
                    PluginFolder = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginRegistries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginRegistries");
        }
    }
}
