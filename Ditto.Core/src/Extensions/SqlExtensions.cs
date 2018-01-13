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
                var relationalTable = entity.Relational();
                //var tableName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(relationalTable.TableName);
                //var tableName = relationalTable.TableName.ToTileCaseExtended();
                var tableName = Regex.Replace(relationalTable.TableName, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0", RegexOptions.Compiled).ToTitleCase().Replace(" ", "");
                relationalTable.TableName = Globals.RegularExpression.SqlUnderscore.Replace(tableName, @"_$1$2").ToLower();

                // properties
                var entityProperties = entity.GetProperties();
                foreach (var property in entityProperties)
                {
                    var relationalProperty = property.Relational();
                    //relationalProperty.ColumnName = Globals.RegularExpression.SqlUnderscore.Replace(relationalProperty.ColumnName, @"_$1$2").ToLower();
                    var propertyName = Regex.Replace(relationalProperty.ColumnName, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0", RegexOptions.Compiled).ToTitleCase().Replace(" ", "");
                    relationalProperty.ColumnName = Globals.RegularExpression.SqlUnderscore.Replace(propertyName, @"_$1$2").ToLower();
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
                    var relationalProperty = property.Relational();
                    if(property.IsPrimaryKey() && property.ClrType == typeof(int) && relationalProperty.ColumnName == "id")
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
