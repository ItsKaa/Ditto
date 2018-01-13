using Ditto.Data.Database;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database
{
    /// <summary>
    /// Database entity with a primary key [id]
    /// </summary>
    public abstract class DbEntity : BaseDbEntity, IDbEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}
