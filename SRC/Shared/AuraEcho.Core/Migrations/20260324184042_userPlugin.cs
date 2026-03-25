using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations
{
    /// <inheritdoc />
    public partial class userPlugin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginRegistries");

            migrationBuilder.CreateTable(
                name: "LocalPlugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Manifest_Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Manifest_Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Author = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_PluginName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Version = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Description = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_EntryAssemblyName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_DefaultViewName = table.Column<string>(type: "TEXT", nullable: true),
                    PluginFolder = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPlugins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPlugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalPluginId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlugins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPlugins_LocalPlugins_LocalPluginId",
                        column: x => x.LocalPluginId,
                        principalTable: "LocalPlugins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPlugins_LocalPluginId",
                table: "UserPlugins",
                column: "LocalPluginId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPlugins");

            migrationBuilder.DropTable(
                name: "LocalPlugins");

            migrationBuilder.CreateTable(
                name: "PluginRegistries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PlanStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PluginFolder = table.Column<string>(type: "TEXT", nullable: false),
                    Manifest_Author = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_DefaultViewName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Description = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_EntryAssemblyName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Manifest_PluginName = table.Column<string>(type: "TEXT", nullable: true),
                    Manifest_Version = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginRegistries", x => x.Id);
                });
        }
    }
}
