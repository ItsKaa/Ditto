using Ditto.Bot.Database.Data;
using System;
using System.Collections.Generic;

namespace Ditto.Bot.Modules.BDO.Data
{
    public struct BdoStatusResult : IEquatable<BdoStatusResult>
    {
        public static readonly BdoStatusResult InvalidResult = new BdoStatusResult()
        {
            Status = BdoServerStatus.Unknown,
            Error = null,
            MaintenanceTime = null
        };

        public BdoServerStatus Status { get; set; }
        public string Error { get; set; }
        public DateTime? MaintenanceTime { get; set; }

        public override bool Equals(object obj)
        {
            if(obj is BdoStatusResult)
            {
                return Equals((BdoStatusResult)obj);
            }
            return base.Equals(obj);
        }
        public bool Equals(BdoStatusResult other)
            => this == other;
        
        public override int GetHashCode()
        {
            var hashCode = -559054895;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Status.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Error);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime?>.Default.GetHashCode(MaintenanceTime);
            return hashCode;
        }

        public static bool operator==(BdoStatusResult left, BdoStatusResult right)
        {
            return left.Status == right.Status
                && left.Error == right.Error
                && left.MaintenanceTime == right.MaintenanceTime;
        }
        public static bool operator!=(BdoStatusResult left, BdoStatusResult right)
            => !(left == right);
    }
}
