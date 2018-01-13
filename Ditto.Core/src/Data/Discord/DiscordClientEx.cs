using Discord;
using Discord.WebSocket;
using Ditto.Extensions;
using System.Threading.Tasks;

namespace Ditto.Data.Discord
{

    public class DiscordClientEx : DiscordSocketClient
    {
        public DiscordClientEx(DiscordSocketConfig config) : base(config)
        {
        }

        public Task SetStatusAsync(UserStatusEx status)
            => SetStatusAsync(status.ToUserStatus());
        
        public Task<IGuildUser> GetGuildUserAsync(IGuild guild, ulong userId)
        {
            return guild?.GetUserAsync(userId);
        }
        public Task<IGuildUser> GetGuildUserAsync(ulong guildId, ulong userId) => GetGuildUserAsync(GetGuild(guildId), userId);
        public Task<IGuildUser> GetGuildUserAsync(IGuild guild, IUser user) => GetGuildUserAsync(guild, user?.Id ?? 0);
        public Task<IGuildUser> GetGuildUserAsync(ulong guildId, IUser user) => GetGuildUserAsync(GetGuild(guildId), user);

        public Task<IGuildUser> GetGuildUserAsync(IGuild guild) => GetGuildUserAsync(guild, CurrentUser);
        public Task<IGuildUser> GetGuildUserAsync(ulong guildId) => GetGuildUserAsync(guildId, CurrentUser);

        public async Task<ChannelPermissions> GetPermissionsAsync(IGuildChannel guildChannel)
        {
            return (await GetGuildUserAsync(guildChannel.Guild))?.GetPermissions(guildChannel) ?? ChannelPermissions.None;
        }
    }
}
