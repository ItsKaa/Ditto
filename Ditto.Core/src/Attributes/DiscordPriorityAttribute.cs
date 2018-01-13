using Discord.Commands;
using System;

namespace Ditto.Attributes
{
    /// <summary> Sets priority of commands </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class DiscordPriorityAttribute : PriorityAttribute
    {
        public DiscordPriorityAttribute(int priority) : base(priority)
        {
        }
    }
}
