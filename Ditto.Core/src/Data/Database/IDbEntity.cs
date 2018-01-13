using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Data.Database
{
    /// <summary>
    /// Database entity with a primary key [id]
    /// </summary>
    public interface IDbEntity : IBaseDbEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        int Id { get; set; }
    }
}
