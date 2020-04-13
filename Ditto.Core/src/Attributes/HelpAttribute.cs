using System;
using System.Collections.Generic;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false)]
    public class HelpAttribute : Attribute
    {
        public string Name { get; set; }
        public string LongDescription { get; set; }
        public string ShortDescription { get; set; }
        public string Extra { get; set; }

        public HelpAttribute(string name, string description, string extra = null)
        {
            Name = name;
            ShortDescription = description;
            LongDescription = description;
            Extra = extra;
        }
    }
}
