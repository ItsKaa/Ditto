using System.Runtime.Serialization;

namespace Ditto.Translation.Data
{
	[DataContract]
	public sealed class Corrections
	{
		[DataMember] public bool TextWasCorrected { get; internal set; }
		[DataMember] public string CorrectedText { get; internal set; }
		[DataMember] public string[] CorrectedWords { get; internal set; }

		internal Corrections() { }
	}
}
