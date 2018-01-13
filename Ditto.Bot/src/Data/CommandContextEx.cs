using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using Ditto.Data.Commands;
using Ditto.Extensions;
using Ditto.Data.Discord;

namespace Ditto.Bot.Data
{
    public class CommandContextEx : CommandContext, ICommandContextEx
    {
        public IGuildUser GuildUser => User as IGuildUser;
        //public string Mention => GuildUser?.Mention ?? User?.Mention ?? "";
        public string Nickname => GuildUser?.Nickname ?? GuildUser?.Username ?? User?.Username ?? "";
        public string GlobalUsername => (User?.Username ?? "") + "#" + (User?.Discriminator ?? "0000");
        public string NicknameAndGlobalUsername => $"{Nickname} ({GlobalUsername})";

        public bool IsBotUserTagged { get; private set; } = false;

        public CommandContextEx(IDiscordClient client, IUserMessage msg) : base(client, msg)
        {
            IsBotUserTagged = null != (
                (Message?.Content?.TrimStart() ?? "")
                .ParseDiscordTags()
                .FirstOrDefault(x => x.IsSuccess && x.Type == DiscordTagType.USER && x.Id == client.CurrentUser?.Id)
            );
        }

        public Task<IUserMessage> EmbedAsync(string message, ContextMessageOption options = ContextMessageOption.None)
            => EmbedAsync(message, null, options);

        public Task<IUserMessage> EmbedAsync(string message, EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None)
        {
            if (false == embedBuilder?.Color.HasValue)
            {
                embedBuilder = embedBuilder.WithOkColour(Guild);
            }

            var error = options.Has(ContextMessageOption.Error);
            var replyUser = options.Has(ContextMessageOption.ReplyUser);

            return Channel.EmbedAsync(
                embedBuilder ?? new EmbedBuilder()
                {
                    Description = string.Format("{0}{1}{2}",
                        error ? "💢 " : "",
                        replyUser ? $"{User?.Mention} " : "",
                        message
                    ),
                    Color = (error ? Ditto.Cache.Db.EmbedErrorColour(Guild)
                        : Ditto.Cache.Db.EmbedColour(Guild)
                    )
                },
                embedBuilder == null ? string.Empty : message
            );
        }

        public Task<IUserMessage> EmbedAsync(string format, params object[] args)
             => EmbedAsync(string.Format(format, args));

        public Task<IUserMessage> EmbedAsync(ContextMessageOption options, string format, params object[] args)
            => EmbedAsync(string.Format(format, args), options);

        public Task<IUserMessage> EmbedAsync(EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None)
            => EmbedAsync("", embedBuilder, options);
    }
}
