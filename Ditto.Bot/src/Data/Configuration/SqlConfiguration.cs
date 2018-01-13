using Ditto.Attributes;
using Ditto.Bot.Services;
using Ditto.Data.Configuration;
using System;

namespace Ditto.Bot.Data.Configuration
{
    [Serializable]
    public class SqlConfiguration : ConfigurationXml<SqlConfiguration>
    {
        [Comment("\n <{0}>{1}</{0}>\n <{0}>{2}</{0}>\n",
            nameof(Type),
            nameof(DatabaseType.Sqlite),
            nameof(DatabaseType.Mysql)
        )]
        public DatabaseType Type { get; set; } = DatabaseType.Sqlite;

        [Comment("\n  If needed, enter a connection string.\n  see https://www.connectionstrings.com/sql-server/\n")]
        public string ConnectionString { get; set; } = "Server=localhost;Port=3306;Database=ditto;User Id=USERNAME;Password=PASSWORD;SSL Mode=Preferred";
    }
}
