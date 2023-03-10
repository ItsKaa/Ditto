using Ditto.Data.Discord;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Ditto.Extensions
{
    public static partial class StringExtensions
    {
        public static IEnumerable<DiscordTagResult> ParseDiscordTags(this string @this)
        {
            // @user
            foreach (Match m in Globals.RegularExpression.DiscordTagUser.Matches(@this))
            {
                if (m.Success)
                    yield return new DiscordTagResult(m, DiscordTagType.USER);
            }

            // @role
            foreach (Match m in Globals.RegularExpression.DiscordTagRole.Matches(@this))
            {
                if (m.Success)
                    yield return new DiscordTagResult(m, DiscordTagType.ROLE);
            }

            // @channel
            foreach (Match m in Globals.RegularExpression.DiscordTagChannel.Matches(@this))
            {
                if (m.Success)
                    yield return new DiscordTagResult(m, DiscordTagType.CHANNEL);
            }
        }
        
        public static DiscordTagResult ParseDiscordUserTag(this string @this)
            => new DiscordTagResult(Globals.RegularExpression.DiscordTagUser.Match(@this), DiscordTagType.USER);

        public static DiscordTagResult ParseDiscordRoleTag(this string @this)
            => new DiscordTagResult(Globals.RegularExpression.DiscordTagUser.Match(@this), DiscordTagType.ROLE);

        public static DiscordTagResult ParseDiscordChannelTag(this string @this)
            => new DiscordTagResult(Globals.RegularExpression.DiscordTagUser.Match(@this), DiscordTagType.CHANNEL);

        public static IEnumerable<DiscordTagResult> ParseDiscordEmojis(this string @this)
        {
            foreach (var m in Globals.RegularExpression.DiscordEmoji.Matches(@this).Where(m => m.Success))
            {
                if (m.Groups["animated"]?.Value == "a")
                    yield return new DiscordTagResult(m, DiscordTagType.EMOJI_ANIMATED);
                else
                    yield return new DiscordTagResult(m, DiscordTagType.EMOJI);
            }
        }
    }
}
