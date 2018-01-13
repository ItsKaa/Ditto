﻿using Discord;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Database.Repositories.Interfaces;
using Ditto.Data.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ditto.Bot.Database.Repositories
{
    public class ConfigRepository : Repository<Config>, IConfigRepository
    {
        private const string _keyEmbedOkColour = "embed_colour_ok";
        private const string _keyEmbedErrorColour = "embed_colour_error";
        private const string _keyEmbedRssColour = "embed_colour_rss";
        private const string _keyPrefix = "prefix";
        private const string _keyBdoMaintenanceChannel = "bdo_maintenance_channel";

        private static ConcurrentDictionary<string, string> _defaultConfigValues = new ConcurrentDictionary<string, string>(new Dictionary<string, string>() {
             {_keyEmbedOkColour , Color.Blue.ToString() },
            {_keyEmbedErrorColour, "#C42F1F" },
            {_keyEmbedRssColour, "#F26522" },
            {_keyPrefix, ">"},
            {_keyBdoMaintenanceChannel, null}
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

        public Config GetEmbedColour(IGuild guild) => GetConfigItem(guild, _keyEmbedOkColour);
        public void SetEmbedColour(IGuild guild, Color colour) => GetEmbedColour(guild).Value = colour.ToString();
        
        public Config GetEmbedErrorColour(IGuild guild) => GetConfigItem(guild, _keyEmbedErrorColour);
        public void SetEmbedErrorColour(IGuild guild, Color colour) => GetEmbedErrorColour(guild).Value = colour.ToString();

        public Config GetPrefix(IGuild guild) => GetConfigItem(guild, _keyPrefix);
        public void SetPrefix(IGuild guild, string prefix) => GetPrefix(guild).Value = prefix;
        
        public Config GetEmbedRssColour(IGuild guild) => GetConfigItem(guild, _keyEmbedRssColour);
        public void SetEmbedRssColour(IGuild guild, Color colour) => GetEmbedRssColour(guild).Value = colour.ToString();
        
        public Config GetBdoMaintenanceChannel(IGuild guild) => GetConfigItem(guild, _keyBdoMaintenanceChannel);
        public void SetBdoMaintenanceChannel(IGuild guild, ulong channelId) => GetBdoMaintenanceChannel(guild).Value = channelId.ToString();
        public void SetBdoMaintenanceChannel(IGuild guild, ITextChannel textChannel) => SetBdoMaintenanceChannel(guild, textChannel.Id);
    }
}
