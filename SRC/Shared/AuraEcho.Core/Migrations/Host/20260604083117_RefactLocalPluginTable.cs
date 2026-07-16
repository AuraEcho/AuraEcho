using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations.Host
{
    /// <inheritdoc />
    public partial class RefactLocalPluginTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPlugins_LocalPlugins_LocalPluginId",
                table: "UserPlugins");

            migrationBuilder.DropTable(
                name: "LocalPlugins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPlugins",
                table: "UserPlugins");

            migrationBuilder.RenameTable(
                name: "UserPlugins",
                newName: "UserPlugin");

            migrationBuilder.RenameIndex(
                name: "IX_UserPlugins_LocalPluginId",
                table: "UserPlugin",
                newName: "IX_UserPlugin_LocalPluginId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPlugin",
                table: "UserPlugin",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "InstalledPlugin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PluginId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PluginType = table.Column<int>(type: "INTEGER", nullable: false),
                    InstallPath = table.Column<string>(type: "TEXT", nullable: true),
                    InstaledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    IsSetup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstalledPlugin", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UserPlugin_InstalledPlugin_LocalPluginId",
                table: "UserPlugin",
                column: "LocalPluginId",
                principalTable: "InstalledPlugin",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPlugin_InstalledPlugin_LocalPluginId",
                table: "UserPlugin");

            migrationBuilder.DropTable(
                name: "InstalledPlugin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPlugin",
                table: "UserPlugin");

            migrationBuilder.RenameTable(
                name: "UserPlugin",
                newName: "UserPlugins");

            migrationBuilder.RenameIndex(
                name: "IX_UserPlugin_LocalPluginId",
                table: "UserPlugins",
                newName: "IX_UserPlugins_LocalPluginId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPlugins",
                table: "UserPlugins",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "LocalPlugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    PluginFolder = table.Column<string>(type: "TEXT", nullable: false),
                    Manifest_Author = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_EntryAssemblyName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Manifest_PluginName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Version = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPlugins", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UserPlugins_LocalPlugins_LocalPluginId",
                table: "UserPlugins",
                column: "LocalPluginId",
                principalTable: "LocalPlugins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
