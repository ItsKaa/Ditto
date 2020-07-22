using System;

namespace Ditto.Translation.Attributes
{
    public class LanguageISOAttribute : Attribute
    {
        public string ISO { get; set; }

        public LanguageISOAttribute(string ISO)
        {
            this.ISO = ISO;
        }
    }
}
