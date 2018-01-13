using Ditto.Attributes;
using Ditto.Data.Feed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TB.ComponentModel;

namespace Ditto.Helpers
{
    public static class FeedHelper
    {
        public struct FeedWithOptionalParameterList
        {
            // This type has no other purpose.
        }

        private static object GetValueFromElements(string name, IEnumerable<XElement> elements, IEnumerable<FeedNamespace> namespaces)
        {
            return elements.FirstOrDefault(e =>
            {
                var elementName = e.Name.LocalName;
                if (!string.IsNullOrEmpty(e.Name.NamespaceName))
                {
                    var ns = namespaces.FirstOrDefault(n => n.Url == e.Name.NamespaceName);
                    if (ns != null && !string.IsNullOrEmpty(ns.Namespace))
                    {
                        elementName = ns.Namespace + ":" + elementName;
                    }
                }
                return elementName.Equals(name, StringComparison.CurrentCultureIgnoreCase);
            })?.Value;
        }

        public static FeedResult<TChannel, TItem> ParseRss<TChannel, TItem>(string url)
            where TItem : FeedItem, new()
            where TChannel : FeedChannel, new()
        {
            var channel = new TChannel();
            var items = new List<TItem>();
            try
            {
                XDocument doc = XDocument.Load(url);
                var propChannel = typeof(TChannel).GetProperties().Where(p => p.GetCustomAttribute<FeedElementAttribute>()?.Ignore != true).ToList();
                var propItem = typeof(TItem).GetProperties().Where(p => p.GetCustomAttribute<FeedElementAttribute>()?.Ignore != true).ToList();

                var namespaces = doc.Root.Attributes().Where(e => e.IsNamespaceDeclaration).Select(e => new FeedNamespace()
                {
                    Namespace = e.Name.LocalName,
                    Url = e.Value
                });
                var rssElements = doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements();
                var channelElements = rssElements.Where(e => e.Name.LocalName != "item");
                var itemsElements = rssElements.Where(e => e.Name.LocalName == "item");

                foreach (var property in propChannel)
                {
                    try
                    {
                        object value = null;
                        var names = property.GetCustomAttribute<FeedElementAttribute>()?.Names ?? new[] { property.Name };
                        foreach (var name in names)
                        {
                            try
                            {
                                value = GetValueFromElements(name, channelElements, namespaces);
                                if (value != null)
                                    break;
                            }
                            catch { }
                        }
                        try
                        {
                            property.SetValue(channel, UniversalTypeConverter.Convert(value, property.PropertyType, ConversionOptions.None | ConversionOptions.AllowDefaultValueIfNull));
                        }
                        catch { }
                    }
                    catch { }
                }
                foreach (var elements in itemsElements.Select(e => e.Elements()))
                {
                    try
                    {
                        var item = new TItem();
                        foreach (var property in propItem)
                        {
                            object value = null;
                            var names = property.GetCustomAttribute<FeedElementAttribute>()?.Names ?? new[] { property.Name };
                            foreach (var name in names)
                            {
                                try
                                {
                                    value = GetValueFromElements(name, elements, namespaces);
                                    if (value != null)
                                        break;
                                }
                                catch { }
                            }
                            try
                            {
                                property.SetValue(item, UniversalTypeConverter.Convert(value, property.PropertyType, ConversionOptions.None | ConversionOptions.AllowDefaultValueIfNull));
                            }
                            catch { }
                        }
                        items.Add(item);
                    }
                    catch { }
                }
            }
            catch { }
            return new FeedResult<TChannel, TItem>(FeedType.RSS, channel, items.OrderBy(e => e.PublishDate));
        }

        public static FeedResult<FeedChannel, TItem> ParseRss<TItem>(string url)
            where TItem : FeedItem, new()
            => ParseRss<FeedChannel, TItem>(url);

        public static FeedResult<TChannel, FeedItem> ParseRss<TChannel>(string url, FeedWithOptionalParameterList _ = default(FeedWithOptionalParameterList))
            where TChannel : FeedChannel, new()
            => ParseRss<TChannel, FeedItem>(url);

        public static FeedResult<FeedChannel, FeedItem> ParseRss(string url)
            => ParseRss<FeedChannel, FeedItem>(url);
    }
}
