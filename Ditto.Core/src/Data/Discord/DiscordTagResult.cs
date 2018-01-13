using System.Text.RegularExpressions;

namespace Ditto.Data.Discord
{
    public class DiscordTagResult
    {
        public static readonly ulong InvalidId = ulong.MaxValue;
        public DiscordTagType Type { get; private set; }
        public ulong Id { get; private set; }
        public int Length { get; private set; }
        public int Index { get; private set; }
        public bool IsSuccess => (Index >= 0 && Id != InvalidId);

        public DiscordTagResult(Match regexMatch, DiscordTagType type)
        {
            if (regexMatch.Success && ulong.TryParse(regexMatch.Groups["id"].Value, out ulong id))
            {
                Id = id;
            }
            else Id = InvalidId;

            Length = regexMatch.Length;
            Index = regexMatch.Index;
            Type = type;
        }
    }
}
