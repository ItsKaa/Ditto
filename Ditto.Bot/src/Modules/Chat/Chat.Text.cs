using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Modules.Admin;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Chat
{
    [Alias("chat")]
    public class ChatText : DiscordTextModule
    {
        public ChatService ChatService { get; }
        public ChatText(DatabaseCacheService cache, DatabaseService database, ChatService chatService) : base(cache, database)
        {
            ChatService = chatService;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, RequireBotTag = false)]
        public async Task Talk([Multiword] string message)
        {
            var response = await ChatService.Talk(Context.Guild, Context.TextChannel, message);
            if (!string.IsNullOrEmpty(response))
            {
                await Context.Channel.SendMessageAsync(response);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, DeleteUserMessage = true)]
        [Priority(1)]
        public Task Insult(IUser user)
        {
            return Insult(user?.Mention);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, DeleteUserMessage = true)]
        [Priority(0)]
        public async Task Insult([Multiword] string name)
        {
            var message = ChatService.Insult(name);
            if (!string.IsNullOrEmpty(message))
            {
                await Context.Channel.SendMessageAsync(message).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        [Alias("purge")]
        public async Task Prune(int count = 100, IUser user = null, [Multiword] string pattern = null)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            await Context.Message.DeleteAsync().ConfigureAwait(false);
            await Task.Delay(100).ConfigureAwait(false);

            if (count > ChatService.PruneConfirmationMessageCount)
            {
                var usedReaction = await Context.SendOptionDialogueAsync(
                    ChatService.GetPruneConfirmationMessage(Context.User, count),
                    new List<string>(), true,
                    new[] { EmotesHelper.GetEmoji(Emotes.WhiteCheckMark), EmotesHelper.GetEmoji(Emotes.NoEntrySign) }, null, 60000, 0
                ).ConfigureAwait(false);
                if(usedReaction != 1)
                {
                    return;
                }
            }

            await ChatService.PruneMessagesAsync(Context.TextChannel, count, user, pattern);
        }

        [Priority(4), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        [Help(null, "Make the bot user send a message.")]
        public async Task Say(
            [Help("channel", "The targeted text channel", optional: true)]
            ITextChannel channel,
            [Help("user", "The user to mention", optional: true)]
            IUser user,
            [Help("message", "The message to write")]
            [Multiword] string message)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Check the channel
            if (channel == null)
            {
                channel = Context.TextChannel;
                if(channel == null)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
            }

            // Check user permissions
            var guildUserPermissions = Context.GuildUser?.GetPermissions(channel);
            if(guildUserPermissions?.ViewChannel != true || guildUserPermissions?.SendMessages != true)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Check bot permissions
            var channelPermissions = await Ditto.Client.GetPermissionsAsync(channel);
            if (!channelPermissions.ViewChannel || !channelPermissions.SendMessages)
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            await channel.SendMessageAsync((user == null ? string.Empty : $"{user?.Mention} ") + message).ConfigureAwait(false);
        }

        [Priority(3), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(IUser user, ITextChannel channel, [Multiword] string message)
            => Say(channel, user, message);

        [Priority(2), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(IUser user, [Multiword] string message)
            => Say(null, user, message);

        [Priority(1), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(ITextChannel channel, [Multiword] string message)
            => Say(channel, null, message);

        [Priority(0), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say([Multiword] string message)
            => Say((ITextChannel)null, null, message);
    }
}
