using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraEcho.Core.Migrations.Host
{
    /// <inheritdoc />
    public partial class AddUserReadAnnouncement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReadAnnouncement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReadVersion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReadAnnouncement", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserReadAnnouncement_UserId_AnnouncementId",
                table: "UserReadAnnouncement",
                columns: new[] { "UserId", "AnnouncementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReadAnnouncement");
        }
    }
}
