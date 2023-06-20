using Discord;
using Ditto.Bot.Data.API.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Helpers
{
    public static class ChatHelper
    {
        public static string Insult(string name)
        {
            var insult = new InsultApi().Insult("");
            return !string.IsNullOrEmpty(insult?.Insult)
                ? name + insult.Insult
                : null;
        }


        public static async Task<int> PruneMessagesAsync(ITextChannel channel, int count, IUser user = null, string pattern = "")
        {
            if (count > 100)
                count = 100;
            else if (count <= 0)
                count = 1;

            var deletedCount = 0;

            // First attempt to delete it by bulk (only 14 days)
            try
            {
                var list14Days = (await channel.GetMessagesAsync(count).ToListAsync())
                    .SelectMany(i => i)
                    .Where(x => !x.IsPinned
                        && Math.Abs((DateTime.Now - x.CreatedAt.UtcDateTime).TotalDays) < 14
                        && x.Flags?.HasFlag(MessageFlags.Ephemeral) == false
                    );

                if (!string.IsNullOrEmpty(pattern))
                {
                    list14Days = list14Days.Where(i => i.Content.Contains(pattern));
                }

                if (user != null)
                {
                    list14Days = list14Days.Where(i => i.Author?.Id == user.Id);
                }

                // Attempt to bulk delete
                await channel.DeleteMessagesAsync(list14Days).ConfigureAwait(false);
                deletedCount += list14Days.Count();

                if (count <= deletedCount)
                    return deletedCount;

                // Wait a little bit because it takes some time and it's not in the async await.
                await Task.Delay(2000).ConfigureAwait(false);
            }
            catch { }

            // Manually delete older messages
            var list = (await channel.GetMessagesAsync(count - deletedCount).ToListAsync())
                .SelectMany(i => i)
            .Where(x => !x.IsPinned);

            if (!string.IsNullOrEmpty(pattern))
            {
                list = list.Where(i => i.Content.Contains(pattern));
            }

            if (user != null)
            {
                list = list.Where(i => i.Author?.Id == user.Id);
            }

            foreach (var msg in list)
            {
                try
                {
                    if (msg.Flags?.HasFlag(MessageFlags.Ephemeral) == false)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                        deletedCount++;
                    }
                }
                catch { }
            }

            return deletedCount;
        }
    }
}
