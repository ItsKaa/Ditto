using Discord;
using Discord.WebSocket;
using Ditto.Extensions;
using System.Linq;
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

        public async Task<bool> CanJoinChannel(IVoiceChannel voiceChannel)
        {
            var permissions = await GetPermissionsAsync(voiceChannel);
            if(voiceChannel.UserLimit.HasValue)
            {
                var userCount = await voiceChannel.GetUsersAsync().CountAsync();
                return permissions.Connect && (voiceChannel.UserLimit < userCount+1);
            }
            return permissions.Connect;
        }
    }
}
