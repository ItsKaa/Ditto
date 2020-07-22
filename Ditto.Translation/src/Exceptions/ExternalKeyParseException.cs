using System;

namespace Ditto.Translation.Exceptions
{
	class ExternalKeyParseException : Exception
	{
		public ExternalKeyParseException()
			:this("External key parse failed") { }

		public ExternalKeyParseException(string message)
			:base(message) { }
	}
}
