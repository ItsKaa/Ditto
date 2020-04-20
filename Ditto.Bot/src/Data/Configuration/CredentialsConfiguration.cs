using Ditto.Attributes;
using Ditto.Data.Configuration;
using System;

namespace Ditto.Bot.Data.Configuration
{
    [Serializable]
    public class CredentialsConfiguration : ConfigurationXml<CredentialsConfiguration>
    {
        public SqlConfiguration Sql { get; set; } = new SqlConfiguration();

        [Comment("\n  Bot/User token\n")]
        public string BotToken { get; set; }

        [Comment("\n  User slave token for linking discord channels.\n")]
        public string UserSlaveToken { get; set; }

        public string GoogleApiKey { get; set; }
        public string GiphyApiKey { get; set; }
        public string CleverbotApiKey { get; set; }
        public string TwitchApiClientId { get; set; }
    }
}
