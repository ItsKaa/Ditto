using Ditto.Attributes;
using System;

namespace Ditto.Data.Feed
{
    /// <summary>
    /// Basic feed item,
    /// see https://validator.w3.org/feed/docs/rss2.html#hrelementsOfLtitemgt
    /// </summary>
    public class FeedItem
    {
        public string Title { get; set; }
        public string Link { get; set; }

        [FeedElement("guid", "link")]
        public string Guid { get; set; }
        //public string Author { get; set; }

        [FeedElement("pubDate")]
        public DateTime PublishDate { get; set; }
    }
}
