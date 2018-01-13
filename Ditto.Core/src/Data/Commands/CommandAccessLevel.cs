using System;

namespace Ditto.Data.Commands
{
    [Flags]
    //public enum Accessibility
    public enum CommandAccessLevel
    {
        /// <summary>
        /// Method name, without any added class names.
        /// e.g.: "search".
        /// </summary>
        Global  = 1 << 1,

        /// <summary>
        /// The declaring class with our without parent classes.
        /// e.g.: "youtube search" or "music youtube search".
        /// </summary>
        Local   = 1 << 2,

        /// <summary>
        /// The parent class(es), without the need of the the defining subclass.
        /// e.g.: "music search", where search is defined in the class "Youtube" that is a sublcass of "Music".
        /// </summary>
        Parents = 1 << 3,
        
        /// <summary>
        /// All of the above
        /// </summary>
        All = (Global | Local | Parents),

        /// <summary>
        /// Either local or parents
        /// </summary>
        LocalAndParents = Local | Parents,
    }
}
