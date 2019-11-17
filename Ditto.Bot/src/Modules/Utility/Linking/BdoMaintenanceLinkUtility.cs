using Cauldron.Core.Collections;
using Discord;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Data.Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    public class BdoMaintenanceLinkUtility : DiscordModule<LinkUtility>
    {
        private static ConcurrentList<Link> _links { get; set; }
        static BdoMaintenanceLinkUtility()
        {
            _links = new ConcurrentList<Link>();
            LinkUtility.TryAddHandler(LinkType.BDO_Maintenance, (link, channel) =>
            {
                if (!_links.Contains(link))
                {
                    foreach (var l in _links.Where(e => e.Id == link.Id))
                    {
                        _links.Remove(l);
                    }
                    _links.Add(link);
                }
                return Task.FromResult(Enumerable.Empty<string>());
            });
        }

        public static IEnumerable<ITextChannel> GetLinkedChannels()
        {
            return _links.Select(e => e.Channel)
                .Where(e => e != null);
        }
    }
}
