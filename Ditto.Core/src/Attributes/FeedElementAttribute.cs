using System;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FeedElementAttribute : Attribute
    {
        public bool Ignore { get; set; } = false;
        public string[] Names { get; }
        
        public FeedElementAttribute()
        {
        }
        public FeedElementAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
