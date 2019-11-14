using System;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EmoteValueAttribute : Attribute
    {
        public string Value { get; set; }
        public EmoteValueAttribute(string value)
        {
            Value = value;
        }
    }
}
