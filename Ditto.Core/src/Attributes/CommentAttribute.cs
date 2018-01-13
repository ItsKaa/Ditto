using System;

namespace Ditto.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CommentAttribute : Attribute
    {
        public string Comment { get; set; }
        public CommentAttribute(string comment)
        {
            Comment = comment;
        }
        public CommentAttribute(string format, params object[] args)
        {
            Comment = string.Format(format, args);
        }
    }
}
