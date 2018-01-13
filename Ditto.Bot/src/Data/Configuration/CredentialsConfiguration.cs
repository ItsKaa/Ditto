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
        public string GoogleApiKey { get; set; }
        public string GiphyApiKey { get; set; }
        public string CleverbotApiKey { get; set; }
    }
}
