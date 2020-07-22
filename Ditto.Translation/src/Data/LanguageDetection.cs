using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Ditto.Translation.Data
{
	[DataContract]
	public sealed class LanguageDetection
	{
		[DataMember]
		public Language Language { get; internal set; }
		[DataMember(EmitDefaultValue = false)]
		public double Confidence { get; internal set; }

		public LanguageDetection(Language language, double confidence)
		{
			Language = language;
			Confidence = confidence;
		}

		public override string ToString()
		{
			return $"{Language.FullName} ({Confidence:F5})";
		}
	}
}
