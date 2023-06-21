using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public static async Task<Stream> GetStreamAsync(string url)
        {
            using var client = new HttpClient();
            return await client.GetStreamAsync(url).ConfigureAwait(false);
        }
        public static Task<Stream> GetStreamAsync(Uri uri)
            => GetStreamAsync(uri.ToString());

        public static Task<string> GetSourceCodeAsync(string url)
        {

            try
            {
                using var client = new HttpClient();
                return client.GetStringAsync(url);
            }
            catch { }
            return Task.FromResult<string>(null);
        }

        public static Task<string> GetSourceCodeAsync(Uri uri)
            => GetSourceCodeAsync(uri.ToString());

        public static string GetTitleFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;
            return Regex.Match(html, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
        }
        public static async Task<string> GetTitle(string url)
            => GetTitleFromHtml(await GetSourceCodeAsync(url).ConfigureAwait(false));
        
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
        public static async Task<string> GetIconUrl(string url)
            => GetIconUrlFromHtml(await GetSourceCodeAsync(url).ConfigureAwait(false));


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

        public static async Task<IEnumerable<HttpMetaItem>> GetMetaInfo(string url)
            => GetMetaInfoFromHtml(await GetSourceCodeAsync(url).ConfigureAwait(false));

        public static IEnumerable<HttpMetaItem> GetMetaInfo(Uri uri)
            => GetMetaInfoFromHtml(uri.ToString());

        public static async Task<string> GetResponseUrlAsync(this string url)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                };
                using var client = new HttpClient(handler);
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                // Slightly weird but the request message is actually the response.
                return response.RequestMessage.RequestUri.AbsoluteUri;
            }
            catch { }
            return null;
        }

        public static async Task<bool> IsImageUrlAsync(this string url)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                };
                using var client = new HttpClient(handler);
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return response.Content?.Headers?.ContentType?.MediaType?.ToLower(CultureInfo.InvariantCulture).StartsWith("image/") == true;
            }
            catch { }
            return false;
        }

        public static Task<bool> IsImageUrlAsync(this Uri uri) => IsImageUrlAsync(uri.ToString());

        /// <summary>
        /// Reads the content from the response message and returns the uncompressed string value.
        /// </summary>
        public static async Task<string> ReadContentAsString(this HttpResponseMessage response)
        {
            // Check whether response is compressed
            if (response.Content.Headers.ContentEncoding.Any(x => x == "gzip"))
            {
                // Decompress manually
                using var s = await response.Content.ReadAsStreamAsync();
                using var decompressed = new GZipStream(s, CompressionMode.Decompress);
                using var rdr = new StreamReader(decompressed);
                return await rdr.ReadToEndAsync();
            }

            // Use standard implementation if not compressed
            return await response.Content.ReadAsStringAsync();
        }

        public static Task<string> GetResponseUrlAsync(this Uri uri)
            => GetResponseUrlAsync(uri.ToString());
    }
}
