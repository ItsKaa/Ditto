﻿// <auto-generated />
using System;
using Ditto.Bot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ditto.Bot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Ditto.Bot.Database.Models.BdoStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateUpdated")
                        .HasColumnName("date_updated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Error")
                        .HasColumnName("error")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime?>("MaintenanceTime")
                        .HasColumnName("maintenance_time")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnName("status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("bdo_status");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Command", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<int>("AccessLevel")
                        .HasColumnName("access_level")
                        .HasColumnType("int");

                    b.Property<string>("AliasesString")
                        .HasColumnName("aliases")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool>("Enabled")
                        .HasColumnName("enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("Priority")
                        .HasColumnName("priority")
                        .HasColumnType("int");

                    b.Property<int>("SourceLevel")
                        .HasColumnName("source_level")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("commands");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Config", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<ulong?>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Key")
                        .HasColumnName("key")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Value")
                        .HasColumnName("value")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("config");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Event", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<ulong?>("ChannelId")
                        .HasColumnName("channel_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("CreatorId")
                        .HasColumnName("creator_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("CreatorName")
                        .HasColumnName("creator_name")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("Days")
                        .HasColumnName("days")
                        .HasColumnType("int");

                    b.Property<ulong?>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime?>("LastRun")
                        .HasColumnName("last_run")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MessageBody")
                        .HasColumnName("message_body")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("MessageFooter")
                        .HasColumnName("message_footer")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("MessageHeader")
                        .HasColumnName("message_header")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<TimeSpan>("TimeBegin")
                        .HasColumnName("time_begin")
                        .HasColumnType("time(6)");

                    b.Property<TimeSpan?>("TimeCountdown")
                        .HasColumnName("time_countdown")
                        .HasColumnType("time(6)");

                    b.Property<TimeSpan?>("TimeEnd")
                        .HasColumnName("time_end")
                        .HasColumnType("time(6)");

                    b.Property<TimeSpan?>("TimeOffset")
                        .HasColumnName("time_offset")
                        .HasColumnType("time(6)");

                    b.Property<string>("Title")
                        .HasColumnName("title")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("events");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Link", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<ulong>("ChannelId")
                        .HasColumnName("channel_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("Date")
                        .HasColumnName("date")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("Type")
                        .HasColumnName("type")
                        .HasColumnType("int");

                    b.Property<string>("Value")
                        .HasColumnName("value")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("links");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.LinkItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<string>("Identity")
                        .HasColumnName("identity")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("LinkId")
                        .HasColumnName("link_id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("LinkId");

                    b.ToTable("link_items");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Module", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<string>("AliasesString")
                        .HasColumnName("aliases")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<ulong?>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("modules");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Playlist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<string>("Creator")
                        .HasColumnName("creator")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Data")
                        .HasColumnName("data")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong?>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("Type")
                        .HasColumnName("type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("playlists");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.PlaylistSong", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<string>("Creator")
                        .HasColumnName("creator")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Data")
                        .HasColumnName("data")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<TimeSpan?>("Length")
                        .HasColumnName("length")
                        .HasColumnType("time(6)");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("PlaylistId")
                        .HasColumnName("playlist_id")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnName("type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("PlaylistId");

                    b.ToTable("playlist_songs");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.Reminder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<ulong?>("ChannelId")
                        .HasColumnName("channel_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Creator")
                        .HasColumnName("creator")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime>("EndTime")
                        .HasColumnName("end_time")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong?>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Message")
                        .HasColumnName("message")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool>("Repeat")
                        .HasColumnName("repeat")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong?>("RoleId")
                        .HasColumnName("role_id")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("Self")
                        .HasColumnName("self")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("StartTime")
                        .HasColumnName("start_time")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong?>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("reminders");
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.LinkItem", b =>
                {
                    b.HasOne("Ditto.Bot.Database.Models.Link", "Link")
                        .WithMany("Links")
                        .HasForeignKey("LinkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Ditto.Bot.Database.Models.PlaylistSong", b =>
                {
                    b.HasOne("Ditto.Bot.Database.Models.Playlist", null)
                        .WithMany("Songs")
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
