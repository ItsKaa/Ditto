using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ditto.Extensions
{
    public static partial class StringExtensions
    {
        public static string TrimTo(this string @string, int maxLength, bool hideDots = false)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), $"Argument {nameof(maxLength)} can't be negative.");
            else if (maxLength == 0)
                return string.Empty;
            else if (maxLength <= 3) // dots = 3
                return string.Concat(@string.Select(c => '.'));
            if (@string.Length < maxLength)
                return @string;
            return string.Concat(@string.Take(maxLength - 3)) + (hideDots ? "" : "...");
        }

        public static string ToTitleCase(this string @string)
        {
            var tokens = @string.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                //tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1);
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1).ToLower();
            }
            return string.Join(" ", tokens);
        }

        public static string ToCamelCase(this string src)
        {
            string[] words = src.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return string.Concat(words.Select(word
                => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        /*
        public static string ToPascalCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Split the string into words.
            string[] words = the_string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = "";
            foreach (string word in words)
            {
                result +=
                    word.Substring(0, 1).ToUpper() +
                    word.Substring(1).ToLower();
            }

            return result;
        }

        public static string ToCamelCase(this string @string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (@string == null || @string.Length < 2)
                return @string;

            // Split the string into words.
            string[] words = @string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
            {
                result +=
                    words[i].Substring(0, 1).ToUpper() +
                    words[i].Substring(1);
            }

            return result;
        }

        public static string ToProperCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Start with the first character.
            string result = the_string.Substring(0, 1).ToUpper();

            // Add the remaining characters.
            for (int i = 1; i < the_string.Length; i++)
            {
                if (char.IsUpper(the_string[i])) result += " ";
                result += the_string[i];
            }

            return result;
        }
        */

        public static string Flatten(this IEnumerable<string> inputs, string seperator = "")
        {
            return String.Join(seperator, inputs);
        }
        public static string Flatten(this IEnumerable<string> inputs, char seperator) => Flatten(inputs, seperator.ToString());


        public static string Between(this string source, string start, string end)
        {
            int startPos = source.IndexOf(start);
            if (startPos < 0)
                return string.Empty;
            startPos += start.Length;

            int endPos = source.IndexOf(end, startPos);
            if (end.Length == 0)
                endPos = source.Length;
            else if (endPos < 0)
                return string.Empty; // end not found

            var result = source.Substring(startPos, endPos - startPos);
            return result;
        }

        public static string From(this string source, string start)
        {
            int startPos = source.IndexOf(start);
            if (startPos < 0)
                return string.Empty;
            startPos += start.Length;

            int endPos = source.Length;
            var result = source.Substring(startPos, endPos - startPos);
            return result;
        }

        public static string Repeat(this string source, int amount)
            => new StringBuilder()
            .Insert(0, source, amount)
            .ToString();
    }
}
