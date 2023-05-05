using Discord;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Database.Repositories.Interfaces;
using Ditto.Data.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Ditto.Bot.Database.Repositories
{
    public class ConfigRepository : Repository<Config>, IConfigRepository
    {
        private const string _keyGlobalCacheChannel = "global_cache_channel";
        private const string _keyEmbedOkColour = "embed_colour_ok";
        private const string _keyEmbedErrorColour = "embed_colour_error";
        private const string _keyEmbedRssColour = "embed_colour_rss";
        private const string _keyEmbedDiscordLinkColour = "embed_colour_discordlink";
        private const string _keyEmbedMusicPlayingColour = "embed_colour_music-playing";
        private const string _keyEmbedMusicPausedColour = "embed_colour_music-paused";
        private const string _keyEmbedTwitchColour = "embed_colour_twitch";
        private const string _keyPrefix = "prefix";
        private const string _keyBdoMaintenanceChannel = "bdo_maintenance_channel";
        private const string _keyBdoNewsIdentifier = "bdo_news_id";

        private static ConcurrentDictionary<string, string> _defaultConfigValues = new ConcurrentDictionary<string, string>(new Dictionary<string, string>() {
            {_keyGlobalCacheChannel, null },
            {_keyEmbedOkColour , Color.Blue.ToString() },
            {_keyEmbedErrorColour, "#C42F1F" },
            {_keyEmbedRssColour, "#F26522" },
            {_keyEmbedDiscordLinkColour, "#5971BF" },
            {_keyEmbedMusicPlayingColour, "#735bc1" }, //#763ba5
            {_keyEmbedMusicPausedColour, "#666666" },
            {_keyEmbedTwitchColour, "#6441A4"},
            {_keyPrefix, ">"},
            {_keyBdoMaintenanceChannel, null},
            {_keyBdoNewsIdentifier, "0" }
        });
        
        public ConfigRepository(DbContext dbContext) : base(dbContext)
        {
        }

        /// <summary>
        /// Retrieves a Config key/value pair from the database based on the provided key and guild, should this not exist,
        /// the default value will be returned instead.
        /// </summary>
        private Config GetConfigItem(IGuild guild, string key)
        {
            var guildId = guild?.Id;
            return
                Get(e => e.Key == key && e.GuildId == guildId)
                ?? new Config()
                {
                    GuildId = guildId,
                    Key = key,
                    Value = _defaultConfigValues[key]
                };
        }

        /// <summary>
        /// Retrieves or adds a Config key/value pair from the database based on the provided key and guild,
        /// much less practical.
        /// </summary>
        private Config GetOrAddConfigItem(IGuild guild, string key)
        {
            ulong? guildId = guild?.Id;
            var item = GetOrAdd(e => e.Key == key && e.GuildId == guildId,
                new Config()
                {
                    GuildId = guildId,
                    Key = key,
                    Value = _defaultConfigValues[key],
                }
            );
            Context.SaveChanges();
            return item;
        }

        public Config GetGlobalCacheChannel() => GetConfigItem(null, _keyGlobalCacheChannel);
        private Config GetOrAddGlobalCacheChannel() => GetOrAddConfigItem(null, _keyGlobalCacheChannel);
        public void SetGlobalCacheChannel(ulong channelId) => GetOrAddGlobalCacheChannel().Value = channelId.ToString();
        public void SetGlobalCacheChannel(ITextChannel textChannel) => SetGlobalCacheChannel(textChannel.Id);

        public Config GetEmbedColour(IGuild guild) => GetConfigItem(guild, _keyEmbedOkColour);
        private Config GetOrAddEmbedColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedOkColour);
        public void SetEmbedColour(IGuild guild, Color colour) => GetOrAddEmbedColour(guild).Value = colour.ToString();
        
        public Config GetEmbedErrorColour(IGuild guild) => GetConfigItem(guild, _keyEmbedErrorColour);
        private Config GetOrAddEmbedErrorColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedErrorColour);
        public void SetEmbedErrorColour(IGuild guild, Color colour) => GetOrAddEmbedErrorColour(guild).Value = colour.ToString();

        public Config GetPrefix(IGuild guild) => GetConfigItem(guild, _keyPrefix);
        private Config GetOrAddPrefix(IGuild guild) => GetOrAddConfigItem(guild, _keyPrefix);
        public void SetPrefix(IGuild guild, string prefix) => GetOrAddPrefix(guild).Value = prefix;
        
        public Config GetEmbedRssColour(IGuild guild) => GetConfigItem(guild, _keyEmbedRssColour);
        private Config GetOrAddEmbedRssColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedRssColour);
        public void SetEmbedRssColour(IGuild guild, Color colour) => GetOrAddEmbedRssColour(guild).Value = colour.ToString();

        public Config GetEmbedDiscordLinkColour(IGuild guild) => GetConfigItem(guild, _keyEmbedDiscordLinkColour);
        private Config GetOrAddEmbedDiscordLinkColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedDiscordLinkColour);
        public void SetEmbedDiscordLinkColour(IGuild guild, Color colour) => GetOrAddEmbedRssColour(guild).Value = colour.ToString();

        public Config GetEmbedMusicPlayingColour(IGuild guild) => GetConfigItem(guild, _keyEmbedMusicPlayingColour);
        private Config GetOrAddEmbedMusicPlayingColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedMusicPlayingColour);
        public void SetEmbedMusicPlayingColour(IGuild guild, Color colour) => GetOrAddEmbedMusicPlayingColour(guild).Value = colour.ToString();

        public Config GetEmbedMusicPausedColour(IGuild guild) => GetConfigItem(guild, _keyEmbedMusicPausedColour);
        private Config GetOrAddEmbedMusicPausedColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedMusicPausedColour);
        public void SetMusicPausedColour(IGuild guild, Color colour) => GetOrAddEmbedMusicPausedColour(guild).Value = colour.ToString();

        public Config GetEmbedTwitchLinkColour(IGuild guild) => GetConfigItem(guild, _keyEmbedTwitchColour);
        private Config GetOrAddEmbedTwitchLinkColour(IGuild guild) => GetOrAddConfigItem(guild, _keyEmbedTwitchColour);
        public void SetTwitchLinkColour(IGuild guild, Color colour) => GetOrAddEmbedTwitchLinkColour(guild).Value = colour.ToString();
        

        public Config GetBdoMaintenanceChannel(IGuild guild) => GetConfigItem(guild, _keyBdoMaintenanceChannel);
        private Config GetOrAddBdoMaintenanceChannel(IGuild guild) => GetOrAddConfigItem(guild, _keyBdoMaintenanceChannel);
        public void SetBdoMaintenanceChannel(IGuild guild, ulong channelId) => GetOrAddBdoMaintenanceChannel(guild).Value = channelId.ToString();
        public void SetBdoMaintenanceChannel(IGuild guild, ITextChannel textChannel) => SetBdoMaintenanceChannel(guild, textChannel.Id);

        public Config GetBdoNewsIdentifier(IGuild guild) => GetConfigItem(guild, _keyBdoNewsIdentifier);
        private Config GetOrAddBdoNewsIdentifier(IGuild guild) => GetOrAddConfigItem(guild, _keyBdoNewsIdentifier);
        public void SetBdoNewsIdentifier(IGuild guild, ulong identifier) => GetOrAddBdoNewsIdentifier(guild).Value = identifier.ToString();
    }
}
