using System;

namespace Ditto.Attributes
{

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        public bool Ignored { get; set; } = true;
        public IgnoreAttribute()
        {
        }
    }
}
