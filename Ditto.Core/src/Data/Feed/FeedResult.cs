using System.Collections.Generic;
using System.Linq;

namespace Ditto.Data.Feed
{
    public class FeedResult<TChannel, TItem>
        where TChannel : FeedChannel, new()
        where TItem: FeedItem, new()
    {
        public FeedType Type { get; set; }
        public TChannel Channel { get; private set; }
        public IEnumerable<TItem> Items { get; private set; }
        
        public FeedResult()
        {
            Channel = null;
            Items = Enumerable.Empty<TItem>();
        }

        internal FeedResult(FeedType type, TChannel channel, IEnumerable<TItem> items)
        {
            Type = type;
            Channel = channel;
            Items = items;
        }
    }
}
