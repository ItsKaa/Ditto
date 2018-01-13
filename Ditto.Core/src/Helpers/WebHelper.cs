using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ditto.Helpers
{
    public class HttpMetaItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public static class WebHelper
    {
        public static bool IsValidWebsite(string uri)
        {
            return Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static Uri ToUri(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return uriResult;
            }
            return null;
        }

        public static bool Compare(this Uri left, Uri right)
        {
            return 0 == Uri.Compare(left, right,
                UriComponents.Host | UriComponents.PathAndQuery,
                UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        }


        public static string GetSourceCode(string url)
        {
            try
            {
                using (var client = new WebClient())
                {
                    return client.DownloadString(url);
                }
            }
            catch { }
            return null;
        }
        public static string GetSourceCode(Uri uri)
            => GetSourceCode(uri.ToString());

        public static async Task<string> GetSourceCodeAsync(string url)
        {
            try
            {
                using (var client = new WebClient())
                {
                    return await client.DownloadStringTaskAsync(url).ConfigureAwait(false);
                }
            }
            catch { }
            return null;
        }
        public static Task<string> GetSourceCodeAsync(Uri uri)
            => GetSourceCodeAsync(uri.ToString());


        public static string GetTitleFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;
            return Regex.Match(html, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
        }
        public static string GetTitle(string url)
            => GetTitleFromHtml(GetSourceCode(url));
        
        public static string GetIconUrlFromHtml(string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc.DocumentNode.SelectNodes("//link[@rel='icon']")
                    .Select(e => e.GetAttributeValue("href", null))
                    .Where(e => IsValidWebsite(e)).FirstOrDefault();
            }
            return null;
        }
        public static string GetIconUrl(string url)
            => GetIconUrlFromHtml(GetSourceCode(url));


        public static IEnumerable<HttpMetaItem> GetMetaInfoFromHtml(string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                var metaNodes = doc.DocumentNode.SelectNodes("//meta");
                if (metaNodes != null)
                {
                    return metaNodes.Select(m => new HttpMetaItem()
                    {
                        Name = m.GetAttributeValue("property", null) ?? m.GetAttributeValue("name", null),
                        Value = m.GetAttributeValue("content", null) ?? m.GetAttributeValue("value", null)
                    }).Where(m => !string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(m.Value));
                }
            }
            return Enumerable.Empty<HttpMetaItem>();
        }

        public static IEnumerable<HttpMetaItem> GetMetaInfo(string url)
            => GetMetaInfoFromHtml(GetSourceCode(url));

        public static IEnumerable<HttpMetaItem> GetMetaInfo(Uri uri)
            => GetMetaInfoFromHtml(uri.ToString());
    }
}
