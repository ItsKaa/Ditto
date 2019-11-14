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
        string Nickname { get; }
        string GlobalUsername { get; }
        string NicknameAndGlobalUsername { get; }
        IGuildUser GuildUser { get; }
        //string Mention { get; }
        bool IsBotUserTagged { get; }

        Task<IUserMessage> EmbedAsync(string message, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
        Task<IUserMessage> EmbedAsync(string message, EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
        Task<IUserMessage> EmbedAsync(string format, params object[] args);
        Task<IUserMessage> EmbedAsync(ContextMessageOption options, string format, params object[] args);
        Task<IUserMessage> EmbedAsync(EmbedBuilder embedBuilder, ContextMessageOption options = ContextMessageOption.None, RetryMode retyMode = RetryMode.AlwaysRetry);
    }
}
