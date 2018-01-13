using Ditto.Bot.Database.Data;
using System;

namespace Ditto.Bot.Database.Models
{
    public class BdoStatus : DbEntity
    {
        public BdoServerStatus Status { get; set; }
        public DateTime? MaintenanceTime { get; set; }
        public string Error { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
