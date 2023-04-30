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
        public ulong BotOwnerDiscordId { get; set; } = 156746832381345792;

        [Comment("\n  Sets the time, in seconds to wait for a connection/event to complete before aborting\n")]
        public double Timeout { get; set; } = 500;
        
        public PathConfiguration Paths { get; set; } = new PathConfiguration();
        public CredentialsConfiguration Credentials { get; set; } = new CredentialsConfiguration();
        public CacheConfiguration Cache { get; set; } = new CacheConfiguration();


        [Comment("\n  Sets the proxy settings, used for outgoing data, such as google translate.\n")]
        public ProxySettings ProxySettings { get; set; } = new ProxySettings()
        {
            Enabled = false,
            Host = "localhost",
            Port = 8080,
            Username = string.Empty,
            Password = string.Empty,
        };
    }

    [Serializable]
    public struct ProxySettings
    {
        public bool Enabled { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
