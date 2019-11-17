using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Ditto.Data.Commands
{
    [Flags]
    public enum ContextMessageOption
    {
        None        = 1 << 0,
        ReplyUser   = 1 << 1,
        Error       = 1 << 2,

        ReplyWithError = ReplyUser | Error,
    }
    
    public interface ICommandContextEx : ICommandContext
    {
        /// <summary>
        /// The <paramref name="User" /> nickname.
        /// </summary>
        string Nickname { get; }
        /// <summary>
        /// The <paramref name="User" /> name and disciminator, e.g. Kaa#2195.
        /// </summary>
        string GlobalUsername { get; }
        /// <summary>
        /// The <paramref name="Nickname" /> or User.Username and <paramref name="GlobalUsername" />. e.g. "(Kaa) Kaa#2195"
        /// </summary>
        string NicknameAndGlobalUsername { get; }

        /// <summary>
        /// The User casted as a GuildUser, this could be null.
        /// </summary>
        IGuildUser GuildUser { get; }
        /// <summary>
        /// The Channel casted as a TextChannel, this could be null.
        /// </summary>
        ITextChannel TextChannel { get; }

        /// <summary>
        /// Proper specifies whether or not the command message starts with the prefix value.
        /// This is used in combination of <paramref name="IsBotUserTagged" /> to verify if a command is valid.
        /// </summary>
        bool IsProperCommand { get; }
        /// <summary>
        /// True when the bot user account is tagged in the command message.
        /// </summary>
        bool IsBotUserTagged { get; }


        Task<IUserMessage> EmbedAsync(string message, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
        Task<IUserMessage> EmbedAsync(string message, EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
        Task<IUserMessage> EmbedAsync(string format, params object[] args);
        Task<IUserMessage> EmbedAsync(ContextMessageOption options, string format, params object[] args);
        Task<IUserMessage> EmbedAsync(EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
        Task ApplyResultReaction(CommandResult result);
    }
}
