using Ditto.Bot.Database.Models;
using Ditto.Data;
using Ditto.Extensions;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.IO;
using System.Runtime.InteropServices;

namespace Ditto.Bot.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base()
        {
        }
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //// Used for migrations:
            // Migration guide: https://blogs.msdn.microsoft.com/adonet/2012/02/09/ef-4-3-code-based-migrations-walkthrough/
            // NuGet Commands:
            // * Add-Migration [name]
            // * Remove-Migration
#if MIGRATION
            //// Sqlite
            //var filePath = BaseClass.GetProperPathAndCreate($"{Globals.AppDirectory}/data/KaaBot.db");
            //optionsBuilder.UseSqlite($"Data Source=\"{filePath}\"");

            //// Mysql
            optionsBuilder.UseMySql(new MySqlConnectionStringBuilder()
            {
                Server = "localhost",
                Port = 3306,
                Database = "ditto",
                UserID = "root",
                Password = "",
                SslMode = MySqlSslMode.Preferred,
            }.ConnectionString
            );
#endif
            base.OnConfiguring(optionsBuilder);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Config>();

            modelBuilder.Entity<Command>()
                .PluralTableName();

            modelBuilder.Entity<Reminder>()
                .PluralTableName();

            modelBuilder.Entity<Playlist>()
                .PluralTableName()
                .HasMany(p => p.Songs)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaylistSong>()
                .PluralTableName();

            modelBuilder.Entity<Module>()
                .PluralTableName();
            
            modelBuilder.Entity<Link>()
                .PluralTableName()
                .HasMany(r => r.Links)
                .WithOne(r => r.Link)
                .IsRequired()
                .HasForeignKey(r => r.LinkId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LinkItem>()
                .PluralTableName();
            
            modelBuilder.Entity<BdoStatus>();
            

            modelBuilder.UseUnderscoreNameConvention();
            modelBuilder.SetIdentities();
        }
    }
}
