using System;

namespace Ditto.Bot.Modules.Admin.Data
{
    public struct BuildInfo
    {
        public BuildInfo(string localHash, string remoteHash)
        {
            LocalHash = localHash;
            RemoteHash = remoteHash;
        }

        public string LocalHash { get; set; }
        public string RemoteHash { get; set; }
        public bool IsEqual => LocalHash?.Equals(RemoteHash, StringComparison.CurrentCultureIgnoreCase) ?? false;
    }
}
