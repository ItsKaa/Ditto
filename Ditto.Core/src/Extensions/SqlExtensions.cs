using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.RegularExpressions;

namespace Ditto.Extensions
{
    public static class SqlExtensions
    {   
        public static void UseUnderscoreNameConvention(this ModelBuilder @this, bool table = true, bool properties = true)
        {
            foreach (IMutableEntityType entity in @this.Model.GetEntityTypes())
            {
                // table name
                var tableName = Regex.Replace(entity.GetTableName(), @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0", RegexOptions.Compiled).ToTitleCase().Replace(" ", "");
                entity.SetTableName(Globals.RegularExpression.SqlUnderscore.Replace(tableName, @"_$1$2").ToLower());

                // properties
                var entityProperties = entity.GetProperties();
                foreach (var property in entityProperties)
                {
                    var propertyName = Regex.Replace(property.GetColumnName(), @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0", RegexOptions.Compiled).ToTitleCase().Replace(" ", "");
                    property.SetColumnName(Globals.RegularExpression.SqlUnderscore.Replace(propertyName, @"_$1$2").ToLower());
                }
            }

        }
        public static void SetIdentities(this ModelBuilder @this)
        {
            foreach (IMutableEntityType entity in @this.Model.GetEntityTypes())
            {
                var properties = entity.GetProperties();
                foreach (var property in properties)
                {
                    if (property.IsPrimaryKey()
                        && property.ClrType == typeof(int)
                        && string.Equals(property.GetColumnName(), "id", System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        var exist = entity.FindIndex(property);
                        if (exist == null)
                        {
                            entity.AddIndex(property).IsUnique = true;
                        }
                    }
                }
            }
        }
        
        public static EntityTypeBuilder<TEntity> PluralTableName<TEntity>(this EntityTypeBuilder<TEntity> entity) where TEntity: class
            => entity.ToTable(typeof(TEntity).Name + "s");
    }
}
