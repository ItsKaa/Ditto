﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using Ditto.Data.Commands;
using Ditto.Extensions;
using Ditto.Data.Discord;
using Discord.WebSocket;
using Ditto.Extensions.Discord;

namespace Ditto.Bot.Data.Discord
{
    public class CommandContextEx : ICommandContext, ICommandContextEx
    {
        public IDiscordClient Client { get; set; }
        public IGuild Guild { get; set; }
        public IMessageChannel Channel { get; set; }
        public IUser User { get; set; }
        public IUserMessage Message { get; set; }
        public bool IsPrivate => Channel is IPrivateChannel;
        public ITextChannel TextChannel { get; set; }

        public IGuildUser GuildUser => User as IGuildUser;
        //public string Mention => GuildUser?.Mention ?? User?.Mention ?? "";
        public string Nickname => GuildUser?.Nickname ?? GuildUser?.Username ?? User?.Username ?? "";
        public string GlobalUsername => (User?.Username ?? "") + "#" + (User?.Discriminator ?? "0000");
        public string NicknameAndGlobalUsername => $"{Nickname} ({GlobalUsername})";
        public bool IsBotUserTagged { get; private set; } = false;
        public bool IsProperCommand { get; private set; } = false;

        public CommandContextEx(IDiscordClient client, IUserMessage msg)
        {
            Client = client;
            Guild = (msg?.Channel as IGuildChannel)?.Guild;
            Channel = msg?.Channel;
            User = msg?.Author;
            Message = msg;
            TextChannel = msg?.Channel as ITextChannel;

            // Check if the bot user is tagged
            var botTagged = (Message?.Content?.TrimStart() ?? "").ParseDiscordTags()
                .FirstOrDefault(x => x.IsSuccess
                    && x.Type == DiscordTagType.USER
                    && x.Id == client?.CurrentUser?.Id
            );
            IsBotUserTagged = (botTagged != null);

            // Check if the message contains the configured prefix, see 'IsPropertCommand'.
            var config = Ditto.Database.Do(uow => uow.Configs.GetPrefix(Guild), complete: false);
            if (!string.IsNullOrEmpty(config?.Value))
            {
                var content = Message.Content;
                if (botTagged?.IsSuccess == true)
                {
                    content = content.Remove(botTagged.Index, botTagged.Length).TrimStart();
                }

                IsProperCommand = content.TrimStart().StartsWith(config.Value) == true;
            }
        }

        public Task<IUserMessage> EmbedAsync(string message, EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retryMode = RetryMode.AlwaysRetry)
            => Channel.EmbedAsync(message, embedBuilder, Guild, options, User, retryMode: retryMode);

        public Task<IUserMessage> EmbedAsync(string message, ContextMessageOption options = ContextMessageOption.None, RetryMode retryMode = RetryMode.AlwaysRetry)
            => EmbedAsync(message, null, options, retryMode: retryMode);
        
        public Task<IUserMessage> EmbedAsync(string format, params object[] args)
             => EmbedAsync(string.Format(format, args));

        public Task<IUserMessage> EmbedAsync(ContextMessageOption options, string format, params object[] args)
            => EmbedAsync(string.Format(format, args), options);

        public Task<IUserMessage> EmbedAsync(EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retryMode = RetryMode.AlwaysRetry)
            => EmbedAsync("", embedBuilder, options, retryMode: retryMode);

        public async Task ApplyResultReaction(CommandResult result)
        {
            if (Message != null)
            {
                await Message.SetResultAsync(result).ConfigureAwait(false);
            }
        }
    }
}
