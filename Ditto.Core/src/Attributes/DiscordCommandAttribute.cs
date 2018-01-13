using Discord;
using Ditto.Data.Commands;
using System;
using System.Threading.Tasks;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DiscordCommandAttribute : Attribute
    {
        public CommandSourceLevel SourceLevel { get; private set; }
        public CommandAccessLevel AccessLevel { get; private set; }
        public bool RequireBotTag { get; set; } = false;
        public bool AcceptBotTag { get; set; } = true;

        public DiscordCommandAttribute(CommandSourceLevel sourceLevel = CommandSourceLevel.All,
            CommandAccessLevel accessLevel = CommandAccessLevel.Local,
            bool acceptBotTag = true,
            bool requireBotTag = false)
        {
            SourceLevel = sourceLevel;
            AccessLevel = accessLevel;
            RequireBotTag = requireBotTag;
            AcceptBotTag = acceptBotTag;
        }
        
        public DiscordCommandAttribute WithSourceLevel(CommandSourceLevel sourceLevel)
        {
            SourceLevel = sourceLevel;
            return this;
        }
        public DiscordCommandAttribute WithAccessLevel(CommandAccessLevel accessLevel)
        {
            AccessLevel = accessLevel;
            return this;
        }
        public DiscordCommandAttribute WithBotTag(bool require, bool accept = true)
        {
            RequireBotTag = require;
            AcceptBotTag = accept;
            return this;
        }

        public Task<ConditionResult> VerifyAsync(ICommandContextEx context)
        {
            if (RequireBotTag && !context.IsBotUserTagged)
                return Task.FromResult(ConditionResult.FromError("Invalid use of command, @tag required.", true));
            else if(!AcceptBotTag && context.IsBotUserTagged)
                return Task.FromResult(ConditionResult.FromError("Invalid use of command, @tag is not accepted.", true));

            var valid = false;
            if (!valid && SourceLevel.HasFlag(CommandSourceLevel.Guild))
                valid = context.Channel is IGuildChannel;
            if (!valid && SourceLevel.HasFlag(CommandSourceLevel.Group))
                valid = context.Channel is IGroupChannel;
            if (!valid && SourceLevel.HasFlag(CommandSourceLevel.DM))
                valid = context.Channel is IDMChannel;
            return Task.FromResult(valid ? ConditionResult.FromSuccess() : ConditionResult.FromError($"Invalid context for command; accepted contexts: {SourceLevel}", true));
        }
    }
}
