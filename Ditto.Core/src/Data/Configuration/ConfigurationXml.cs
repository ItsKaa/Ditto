using Ditto.Attributes;
using Ditto.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TB.ComponentModel;

namespace Ditto.Data.Configuration
{
    [Serializable]
    public abstract class ConfigurationXml<T> : BaseClass
        where T: class, new()
    {
        public struct OptionalXmlArg
        {
        }
        public const string RootAttribute = "Ditto";
        
        public virtual Task<T> WriteAsync(string file)
        {
            return WriteAsync(file, this as T);
        }

        private static string GetProperFilePath(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir) || !Path.IsPathRooted(dir))
            {
                char pathChar = IsWindows() ? '\\' : '/';
                filePath = $"{Globals.AppDirectory}{pathChar}{dir}{pathChar}{fileName}";
                dir = Path.GetDirectoryName(filePath);
            }
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return filePath;
        }

        private static async Task WritePropertiesAsync(object configuration, XmlWriter xmlWriter, IEnumerable<PropertyInfo> properties, int level = 1)
        {
            foreach (var property in properties)
            {
                var subProperties = property.PropertyType.GetProperties();
                if (property.PropertyType != typeof(String) && subProperties != null && subProperties.Length > 0)
                {
                    if (property.IsDefined(typeof(CommentAttribute)))
                    {
                        var attribute = property.GetCustomAttribute(typeof(CommentAttribute)) as CommentAttribute;
                        if (!string.IsNullOrEmpty(attribute.Comment))
                        {
                            await xmlWriter.WriteCommentAsync(attribute.Comment);
                        }
                    }
                    await xmlWriter.WriteStartElementAsync("", property.Name, "");
                    await WritePropertiesAsync(property.GetValue(configuration), xmlWriter, subProperties, level+1);
                    await xmlWriter.WriteEndElementAsync();
                }
                else
                {
                    if (property.IsDefined(typeof(CommentAttribute)))
                    {
                        var attribute = property.GetCustomAttribute(typeof(CommentAttribute)) as CommentAttribute;
                        var comment = attribute.Comment;
                        if (!string.IsNullOrEmpty(comment))
                        {
                            if(xmlWriter.Settings.Indent)
                            {
                                comment = comment.Replace("\n", $"\n{xmlWriter.Settings.IndentChars.Repeat(level)}");
                            }
                            await xmlWriter.WriteCommentAsync(comment);
                        }
                    }
                    //xmlWriter.WriteElementString(property.Name, property.GetValue(configuration, null).ToString());
                    await xmlWriter.WriteStartElementAsync("", property.Name, "");
                    await xmlWriter.WriteStringAsync((property.GetValue(configuration, null) ?? "").ToString());
                    await xmlWriter.WriteEndElementAsync();
                }
            }
        }
        
        public static async Task<T> WriteAsync(string file, T configuration)
        {
            file = GetProperFilePath(file);

            //var xmlSerializer = new XmlSerializer(configuration.GetType(), RootAttribute);
            //using (StreamWriter stream = File.CreateText(file))
            //{
            //    xmlSerializer.Serialize(stream, configuration);
            //}

            using (var xmlWriter = XmlWriter.Create(file, new XmlWriterSettings()
            {
                WriteEndDocumentOnClose = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false,
                CheckCharacters = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false,
                Async = true,
                CloseOutput = true
                //DoNotEscapeUriAttributes = true,
            }))
            {
                const string xmlns = "http://www.w3.org/2000/xmlns/";
                const string xsi = "http://www.w3.org/2001/XMLSchema-instance";
                const string xsd = "http://www.w3.org/2001/XMLSchema";

                await xmlWriter.WriteStartDocumentAsync();
                await xmlWriter.WriteStartElementAsync("", RootAttribute, "");
                await xmlWriter.WriteAttributeStringAsync("xmlns", "xsi", xmlns, xsi);
                await xmlWriter.WriteAttributeStringAsync("xmlns", "xsd", xmlns, xsd);

                // Only find properties with a public getter and setter
                await WritePropertiesAsync(configuration, xmlWriter,
                    typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes<IgnoreAttribute>().FirstOrDefault()?.Ignored != true)
                    .Where(p => p.CanRead && p.CanWrite)
                );
                await xmlWriter.WriteEndElementAsync();
            }
            return configuration;
        }

        public static async Task<T> ReadAsync(string filePath)
        {
            filePath = GetProperFilePath(filePath);
            if(!File.Exists(filePath))
            {
                // save defaults
                try
                {
                    await WriteAsync(filePath, new T());
                }
                catch (Exception ex)
                {
                    Log.Error("Could not save the default settings", ex);
                }
            }

            //var xmlSerializer = new XmlSerializer(typeof(T), RootAttribute);
            //using (StreamReader stream = File.OpenText(filePath))
            //{
            //    return (T)xmlSerializer.Deserialize(stream);
            //}
            
            var configuration = new T();
            using (var xmlReader = XmlReader.Create(filePath, new XmlReaderSettings()
            {
                ConformanceLevel = ConformanceLevel.Document,
                CheckCharacters = true,
                CloseInput = true,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                Async = true,
            }))
            {
                await xmlReader.MoveToContentAsync();
                xmlReader.ReadStartElement("Ditto");
                do
                {
                    if(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals(RootAttribute, StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }
                    else if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (await XNode.ReadFromAsync(xmlReader, default(CancellationToken)) is XElement element)
                        {
                            ReadProperties(element, configuration);
                        }
                    }
                    else
                    {
                        await xmlReader.ReadAsync();
                    }
                }
                while (!xmlReader.EOF && xmlReader.ReadState == ReadState.Interactive);
            }
            return configuration;
        }

        public static void ReadProperties(XElement element, object instance)
        {
            var children = element.Descendants();
            if (children.Count() > 0)
            {
                var type = instance.GetType();
                var property = type.GetProperty(element.Name.LocalName);
                if (property.CanRead && property.CanWrite && property.GetCustomAttribute<IgnoreAttribute>()?.Ignored != true)
                {
                    var value = property.GetValue(instance);
                    if (value == null)
                    {
                        property.SetValue(instance, property.PropertyType.CreateInstance());
                        value = property.GetValue(instance);
                    }
                    foreach (var child in children)
                    {
                        ReadProperties(child, value);
                    }
                    property.SetValue(instance, value);
                }
            }
            else
            {
                // Property
                var property = instance.GetType().GetProperty(element.Name.LocalName);
                if (property != null && property.CanRead && property.CanWrite && property.GetCustomAttribute<IgnoreAttribute>()?.Ignored != true)
                {
                    var value = UniversalTypeConverter.Convert(element.Value, property.PropertyType, CultureInfo.CurrentCulture, ConversionOptions.EnhancedTypicalValues);
                    property.SetValue(instance, value);
                }
            }
        }
    }
}
