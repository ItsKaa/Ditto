using Discord;
using Discord.WebSocket;
using Ditto.Data.Discord;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class DiscordClientExtensions
    {
        public static Task SetStatusExAsync(this DiscordSocketClient client, UserStatusEx status)
            => client.SetStatusAsync(status.ToUserStatus());

        public static Task<IGuildUser> GetGuildUserAsync(this IGuild guild, ulong userId) => guild != null
                ? guild.GetUserAsync(userId)
                : Task.FromResult<IGuildUser>(null);

        public static Task<IGuildUser> GetGuildUserAsync(this DiscordSocketClient client, ulong guildId, ulong userId) => GetGuildUserAsync(client.GetGuild(guildId), userId);
        public static Task<IGuildUser> GetGuildUserAsync(this DiscordSocketClient client, ulong guildId, IUser user) => GetGuildUserAsync(client.GetGuild(guildId), user);
        public static Task<IGuildUser> GetGuildUserAsync(this IGuild guild, IUser user) => GetGuildUserAsync(guild, user?.Id ?? 0);

        public static Task<IGuildUser> GetGuildUserAsync(this DiscordSocketClient client, IGuild guild) => GetGuildUserAsync(guild, client.CurrentUser);
        public static Task<IGuildUser> GetGuildUserAsync(this DiscordSocketClient client, ulong guildId) => client.GetGuildUserAsync(guildId, client.CurrentUser);

        public static async Task<ChannelPermissions> GetPermissionsAsync(this DiscordSocketClient client, IGuildChannel guildChannel)
        {
            return (await client.GetGuildUserAsync(guildChannel.Guild))?.GetPermissions(guildChannel) ?? ChannelPermissions.None;
        }

        public static async Task<bool> CanJoinChannel(this DiscordSocketClient client, IVoiceChannel voiceChannel)
        {
            var permissions = await client.GetPermissionsAsync(voiceChannel);
            if (voiceChannel.UserLimit.HasValue)
            {
                var userCount = await voiceChannel.GetUsersAsync().CountAsync();
                return permissions.Connect && (voiceChannel.UserLimit < userCount + 1);
            }
            return permissions.Connect;
        }
    }
}
