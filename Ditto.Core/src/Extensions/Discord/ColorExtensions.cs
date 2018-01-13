using Discord;
using System.Globalization;

namespace Ditto.Extensions
{
    public static class ColorExtensions
    {
        public static Color ToColour(this string @this)
        {
            var argb = uint.Parse(@this.Replace("#", ""), NumberStyles.HexNumber);
            return new Color(argb);
        }
    }
}
