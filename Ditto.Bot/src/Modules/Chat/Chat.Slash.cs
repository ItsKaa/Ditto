using Discord.Interactions;
using Ditto.Data.Discord;
using System.Threading.Tasks;
using Discord;
using Ditto.Bot.Services;

namespace Ditto.Bot.Modules.Chat
{
    [Group("chat", "Group for chat-based commands")]
    public class ChatSlash : DiscordSlashModule
    {
        public ChatSlash(InteractionService interactionService) : base(interactionService) { }
        public const string ButtonIdPurgeConfirmYes = "chat_purge_confirm_yes";
        public const string ButtonIdPurgeConfirmNo  = "chat_purge_confirm_no";

        [SlashCommand("talk", "Chat with the bot (Cleverbot, not ChatGPT)")]
        [DefaultMemberPermissions(GuildPermission.SendMessages)]
        public async Task Talk(
            [Summary(description: "The message you wish to send to the bot")]
            string message)
        {
            var response = await Chat.Talk(Context.Guild, Context.Channel, message);
            await RespondAsync(response);
        }

        [SlashCommand("insult", "Insult a user")]
        [DefaultMemberPermissions(GuildPermission.SendMessages)]
        public Task Insult(
            [Summary(description: "The user you wish to insult")]
            IUser user)
        {
            var name = user.Mention ?? (user as IGuildUser)?.DisplayName ?? (user as IGuildUser)?.Nickname ?? user.Username;
            var message = Chat.Insult(name);
            return !string.IsNullOrEmpty(message)
                ? RespondAsync(message, allowedMentions: AllowedMentions.None)
                : Task.CompletedTask;
        }

        private async Task ExecutePurge(IDiscordInteraction interaction, int count, IUser user, string pattern)
        {
            await interaction.DeferAsync(ephemeral: true);
            var deletedMessageCount = await Chat.PruneMessagesAsync(Context.Channel as ITextChannel, count, user, pattern);
            await interaction.FollowupAsync($"Done! Deleted {deletedMessageCount} messages.", ephemeral: true);
        }

        [SlashCommand("purge", "Purge the messages from the channel")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        public async Task Purge(
            [Summary(description: "Only remoe messages from the specified user")]
            IUser user = null,

            [Summary(description: "Only remove messages that match the pattern")]
            string pattern = null,

            [Summary(description: "The number of messages to delete, up to a maxmimum of 100")]
            int count = 100
            )
        {
            if (count > Chat.PruneConfirmationMessageCount)
            {
                var components = new ComponentBuilder()
                    .WithButton("Yes", ButtonIdPurgeConfirmYes, ButtonStyle.Success)
                    .WithButton("No", ButtonIdPurgeConfirmNo, ButtonStyle.Danger)
                    .Build()
                    ;

                var buttonHandler = new ButtonHandler();
                buttonHandler.AddSingle(ButtonIdPurgeConfirmNo, Context.Interaction.Id, async (msg) =>
                {
                    await msg.DeferAsync(true);
                    await ModifyOriginalResponseAsync((msg) =>
                    {
                        msg.Content = "Cancelled";
                        msg.Components = null;
                    });
                });
                buttonHandler.AddSingle(ButtonIdPurgeConfirmYes, Context.Interaction.Id, async (msg) =>
                {
                    await ModifyOriginalResponseAsync((msg) =>
                    {
                        msg.Content = Chat.GetPruneConfirmationMessage(Context.User, count);
                        msg.Components = null;
                    });
                    await ExecutePurge(msg, count, user, pattern);
                });

                await RespondAsync(text: Chat.GetPruneConfirmationMessage(Context.User, count), components: components, ephemeral: true);
            }
            else
            {
                await ExecutePurge(Context.Interaction, count, user, pattern);
            }
        }

        //private Task Client_ButtonExecuted(SocketMessageComponent arg)
        //{
        //    if (arg.Data?.CustomId == ButtonIdPurgeConfirmNo)
        //    {
        //    }
        //    else if(arg.Data?.CustomId == ButtonIdPurgeConfirmYes)
        //    {
        //        return ExecutePurge();
        //    }

        //    return Task.CompletedTask;
        //}
    }
}
