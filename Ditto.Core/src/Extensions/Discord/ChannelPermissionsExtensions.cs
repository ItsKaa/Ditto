using Discord;

namespace Ditto.Extensions
{
    public static class ChannelPermissionsExtensions
    {
        public static bool HasAccess(this ChannelPermissions @this)
        {
            return @this.Has(ChannelPermission.ViewChannel | ChannelPermission.SendMessages);
        }
    }
}
