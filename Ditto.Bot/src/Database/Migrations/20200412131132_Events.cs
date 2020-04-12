using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditto.Bot.Migrations
{
    public partial class Events : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<ulong>(nullable: true),
                    channel_id = table.Column<ulong>(nullable: true),
                    creator_id = table.Column<ulong>(nullable: true),
                    creator_name = table.Column<string>(nullable: true),
                    time_begin = table.Column<TimeSpan>(nullable: false),
                    time_end = table.Column<TimeSpan>(nullable: true),
                    time_countdown = table.Column<TimeSpan>(nullable: true),
                    time_offset = table.Column<TimeSpan>(nullable: true),
                    days = table.Column<int>(nullable: false),
                    title = table.Column<string>(nullable: true),
                    message_body = table.Column<string>(nullable: true),
                    message_header = table.Column<string>(nullable: true),
                    message_footer = table.Column<string>(nullable: true),
                    last_run = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_id",
                table: "events",
                column: "id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");
        }
    }
}
