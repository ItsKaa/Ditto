using Discord;
using Ditto.Data.Commands;
using System;
using System.Threading.Tasks;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DiscordCommandAttribute : Attribute
    {
        public CommandSourceLevel SourceLevel { get; private set; }
        public CommandAccessLevel AccessLevel { get; private set; }
        public bool RequireBotTag { get; set; } = false;
        public bool AcceptBotTag { get; set; } = false;
        public bool DeleteUserMessage { get; set; } = false;
        public TimeSpan DeleteUserMessageTimer { get; set; } = TimeSpan.Zero;
        
        public DiscordCommandAttribute(CommandSourceLevel sourceLevel = CommandSourceLevel.All,
            CommandAccessLevel accessLevel = CommandAccessLevel.Local,
            bool acceptBotTag = false,
            bool requireBotTag = false,
            bool deleteUserMessage = false,
            double deleteUserMessageAfterSeconds = 0.0)
        {
            SourceLevel = sourceLevel;
            AccessLevel = accessLevel;
            RequireBotTag = requireBotTag;
            AcceptBotTag = acceptBotTag;
            DeleteUserMessage = deleteUserMessage;
            DeleteUserMessageTimer = TimeSpan.FromSeconds(deleteUserMessageAfterSeconds); //new TimeSpan(0,0,0,0, deleteUserMessageAfterMs);
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
        public DiscordCommandAttribute WithDeleteUserMessage(bool delete = true, TimeSpan? timer = null)
        {
            //SourceLevel = sourceLevel;
            DeleteUserMessage = delete;
            DeleteUserMessageTimer = timer ?? TimeSpan.Zero;
            return this;
        }
        public DiscordCommandAttribute WithDeleteUserMessage(TimeSpan timer) => WithDeleteUserMessage(true, timer);

        public Task<ConditionResult> VerifyAsync(ICommandContextEx context)
        {
            // Verify that a tag has been supplied where necessary and that 
            if (RequireBotTag && !context.IsBotUserTagged)
                return Task.FromResult(ConditionResult.FromError("Invalid use of command, @tag required.", true));
            // Tagged commands without a prefix value.
            else if(!context.IsProperCommand && !AcceptBotTag && context.IsBotUserTagged)
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
