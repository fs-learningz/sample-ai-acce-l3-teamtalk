using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTalk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelUserStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelUserStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    LastReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelUserStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelUserStates_ChannelId_UserId",
                table: "ChannelUserStates",
                columns: new[] { "ChannelId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelUserStates");
        }
    }
}
