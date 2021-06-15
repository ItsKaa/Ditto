using Discord;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    public class Permissions : DiscordModule
    {
        static Permissions()
        {
        }

        /// <summary>
        /// Retrieve the bot owner user.
        /// </summary>
        public static Task<Discord.WebSocket.SocketUser> GetBotOwner() => Ditto.Client.DoAsync(client => client.GetUser(Ditto.Settings.BotOwnerDiscordId));
        public static bool IsBotOwner(IUser user) => user.Id == Ditto.Settings.BotOwnerDiscordId;
        public static bool IsBotOwner(ICommandContextEx context) => IsBotOwner(context.User);

        /// <summary>
        /// Determines wether the user has administrator permissions.
        /// </summary>
        public static bool IsAdministrator(IGuildUser guildUser)
            => guildUser?.GuildPermissions.Administrator ?? false;
        public static bool IsAdministrator(ICommandContextEx context) => IsAdministrator(context.GuildUser);

        /// <summary>
        /// Determines wether the user has administrator permissions or is the owner of the bot.
        /// </summary>
        public static bool IsAdministratorOrBotOwner(ICommandContextEx context)
            => IsAdministrator(context) || IsBotOwner(context);
        public static bool IsAdministratorOrBotOwner(IGuildUser guildUser)
            => IsAdministrator(guildUser) || IsBotOwner(guildUser);


        /// <summary>
        /// Determines wether the supplied user has the manage messages permission.
        /// </summary>
        public static bool CanManageMessages(IGuildUser guildUser, IGuildChannel channel)
            => guildUser?.GuildPermissions.ManageMessages == true && channel?.GetPermissionOverwrite(guildUser)?.ManageMessages != PermValue.Deny;
        public static bool CanManageMessages(ICommandContextEx context)
            => CanManageMessages(context.GuildUser, context.TextChannel);

        /// <summary>
        /// Determines wether the bot user has the manage messages permission.
        /// </summary>
        public static Task<bool> CanBotManageMessages(ICommandContextEx context)
            => CanBotManageMessages(context.TextChannel);
        public static async Task<bool> CanBotManageMessages(IGuildChannel channel)
            => CanManageMessages(await channel.Guild.GetCurrentUserAsync().ConfigureAwait(false), channel);

        /// <summary>
        /// Determines whether the bot user has the manage roles permission.
        /// </summary>
        public static async Task<bool> CanBotManageRoles(IGuild guild)
            => (await guild.GetCurrentUserAsync().ConfigureAwait(false))?.GuildPermissions.ManageRoles == true;
        public static Task<bool> CanBotManageRoles(ICommandContextEx context)
            => CanBotManageRoles(context?.Guild);

        /// <summary>
        /// Determines whether the bot user has the manage a channel.
        /// </summary>
        public static async Task<bool> CanBotManageChannel(IGuildChannel textChannel)
        {
            var channelPermissions = (await textChannel.Guild.GetCurrentUserAsync().ConfigureAwait(false))?.GetPermissions(textChannel);
            return channelPermissions?.ManageChannel == true && channelPermissions?.ManageRoles == true;
        }
    }
}
