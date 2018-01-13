using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public class LinkItem : DbEntity
    {
        public int LinkId { get; set; }
        public string Identity { get; set; }

        //[NotMapped]
        //public Link Link
        //{
        //    get => Link;
        //    set => LinkId = value.Id;
        //}

        public Link Link { get; set; }
    }
}
