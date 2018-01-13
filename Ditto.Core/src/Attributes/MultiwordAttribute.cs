using System;

namespace Ditto.Attributes
{
    /// <summary>
    /// This avoids using quotation marks [`'"] in a parameter that requires a string, e.g.: join PPAP Short Version
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class MultiwordAttribute : Attribute
    {
        public MultiwordAttribute()
        {
        }
    }
}
