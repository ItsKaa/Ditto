using Ditto.Attributes;
using Ditto.Data.Configuration;
using System;

namespace Ditto.Bot.Data.Configuration
{
    [Serializable]
    public class SettingsConfiguration : ConfigurationXml<SettingsConfiguration>
    {
        [Comment("\n  <{0}>{1}</{0}>\n  <{0}>{2}</{0}>\n",
            nameof(BotType), nameof(BotType.Bot), nameof(BotType.User)
        )]
        public BotType BotType { get; set; } = BotType.Bot;
        public bool AutoReconnect { get; set; } = true;
        public int AmountOfCachedMessages { get; set; } = 10;
        public ulong BotOwnerDiscordId { get; set; } = 156746832381345792;

        /// <summary>Sets the time, in seconds to wait for a connection/event to complete before aborting</summary>
        [Comment("\n  Sets the time, in seconds to wait for a connection/event to complete before aborting\n")]
        public double Timeout { get; set; } = 500;
        
        /// <summary>Sets how long the internal cache handler should store values, in seconds.</summary>
        [Comment("\n  Sets how long the internal cache handler should store values, in seconds.\n")]
        public double CacheTime { get; set; } = 300;
        
        public int TimeoutInMilliseconds => (int)(Timeout * 60);
        
        public PathConfiguration Paths { get; set; } = new PathConfiguration();
        public CredentialsConfiguration Credentials { get; set; } = new CredentialsConfiguration();
        public BDOSettings BlackDesertOnline { get; set; } = new BDOSettings()
        {
            Email = "",
            Password = "",
            LoginUrl = @"https://www.blackdesertonline.com/launcher/l/api/Login.json?email={0}&password={1}",
            LoginTokenUrl = @"https://www.blackdesertonline.com/launcher/l/api/CreatePlayToken.json?token={0}",
            LauncherUrl = @"http://www.blackdesertonline.com/launcher/l/Launcher.html",
        };

        //public async Task WriteAsync(string file)
        //    => await WriteAsync(file, this);
    }

    [Serializable]
    public struct BDOSettings
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string LoginUrl { get; set; }
        public string LoginTokenUrl { get; set; }
        public string LauncherUrl { get; set; }
    }
}
