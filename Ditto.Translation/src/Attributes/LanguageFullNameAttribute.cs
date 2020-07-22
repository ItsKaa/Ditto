using System;

namespace Ditto.Translation.Attributes
{
    public class LanguageFullNameAttribute : Attribute
    {
        public string FullName { get; set; }

        public LanguageFullNameAttribute(string fullName)
        {
            FullName = fullName;
        }
    }
}
