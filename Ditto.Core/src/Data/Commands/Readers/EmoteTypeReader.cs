using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Ditto.Helpers;

namespace Ditto.Data.Commands.Readers
{
    internal class EmoteTypeReaderList<T> : TypeReader
        where T : IEnumerable<IEmote>
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var emotes = new List<IEmote>();
            foreach (var i in input
                .Replace("`", "")
                .Replace("::", ": :")
                .Split(" ", StringSplitOptions.RemoveEmptyEntries))
            {
                var result = await new EmoteTypeReader<IEmote>().ReadAsync(context, i, services).ConfigureAwait(false);
                if(!result.IsSuccess)
                {
                    return result;
                }
                emotes.Add(result.Values.FirstOrDefault().Value as IEmote);
            }
            return TypeReaderResult.FromSuccess(emotes);
        }
    }

    internal class EmoteTypeReader<T> : TypeReader
        where T : class, IEmote
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            input = input.Replace("`", "").Replace(@"\", "").Trim();

            // Find by name
            foreach(var emote in EmotesHelper.Emotes)
            {
                if(input.Equals($":{EmotesHelper.GetEmojiName(emote)}:", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(EmotesHelper.GetEmoji(emote)));
                }
            }
            
            // Find by unicode value
            foreach (var emote in EmotesHelper.Emojis)
            {
                if(input.Equals(emote.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(emote));
                }
            }

            // Check guild-emotes
            var guildEmote = context.Guild.Emotes.FirstOrDefault(e => string.Equals($"<:{e.Name}:{e.Id}>", input, StringComparison.CurrentCultureIgnoreCase) || string.Equals($"<a:{e.Name}:{e.Id}>", input, StringComparison.CurrentCultureIgnoreCase));
            if (guildEmote != null)
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(guildEmote));
            }
            return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, "Unable to find a valid emote"));
        }
    }
}
