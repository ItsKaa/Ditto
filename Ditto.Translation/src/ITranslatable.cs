namespace Ditto.Translation
{
	public interface ITranslatable
	{
		string OriginalText { get; }
		Language FromLanguage { get; }
		Language ToLanguage { get; }
	}
}
