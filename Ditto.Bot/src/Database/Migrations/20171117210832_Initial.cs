using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Ditto.Bot.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bdo_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    date_updated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    error = table.Column<string>(type: "longtext", nullable: true),
                    maintenance_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bdo_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "commands",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    access_level = table.Column<int>(type: "int", nullable: false),
                    aliases = table.Column<string>(type: "longtext", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    source_level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    key = table.Column<string>(type: "longtext", nullable: true),
                    value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "links",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    aliases = table.Column<string>(type: "longtext", nullable: true),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    name = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    creator = table.Column<string>(type: "longtext", nullable: true),
                    data = table.Column<string>(type: "longtext", nullable: true),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    name = table.Column<string>(type: "longtext", nullable: true),
                    type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    creator = table.Column<string>(type: "longtext", nullable: true),
                    end_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    message = table.Column<string>(type: "longtext", nullable: true),
                    repeat = table.Column<bool>(type: "bit", nullable: false),
                    role_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    self = table.Column<bool>(type: "bit", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "link_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    identity = table.Column<string>(type: "longtext", nullable: true),
                    link_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_link_items_links_link_id",
                        column: x => x.link_id,
                        principalTable: "links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_songs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    creator = table.Column<string>(type: "longtext", nullable: true),
                    data = table.Column<string>(type: "longtext", nullable: true),
                    length = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    name = table.Column<string>(type: "longtext", nullable: true),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_songs", x => x.id);
                    table.ForeignKey(
                        name: "FK_playlist_songs_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bdo_status_id",
                table: "bdo_status",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commands_id",
                table: "commands",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_config_id",
                table: "config",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_link_items_id",
                table: "link_items",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_link_items_link_id",
                table: "link_items",
                column: "link_id");

            migrationBuilder.CreateIndex(
                name: "IX_links_id",
                table: "links",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_modules_id",
                table: "modules",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_songs_id",
                table: "playlist_songs",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_songs_playlist_id",
                table: "playlist_songs",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlists_id",
                table: "playlists",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reminders_id",
                table: "reminders",
                column: "id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bdo_status");

            migrationBuilder.DropTable(
                name: "commands");

            migrationBuilder.DropTable(
                name: "config");

            migrationBuilder.DropTable(
                name: "link_items");

            migrationBuilder.DropTable(
                name: "modules");

            migrationBuilder.DropTable(
                name: "playlist_songs");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "links");

            migrationBuilder.DropTable(
                name: "playlists");
        }
    }
}
