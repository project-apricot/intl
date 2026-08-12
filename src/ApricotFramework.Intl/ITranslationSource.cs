namespace ApricotFramework.Intl;

/// <summary>
/// A set of message templates for a single locale. Several sources may contribute to the same locale;
/// how they combine is up to the <see cref="ITranslationStore"/> consuming them, and
/// <see cref="InMemoryTranslationStore"/> merges them with the last one enumerated winning.
/// </summary>
public interface ITranslationSource
{
    /// <summary>
    /// Gets the locale these messages belong to.
    /// </summary>
    /// <returns>The locale name. Never null.</returns>
    string GetLocale();

    /// <summary>
    /// Gets the message templates, keyed by message id.
    /// </summary>
    /// <returns>The message templates. Never null, but may be empty.</returns>
    IReadOnlyDictionary<string, string> GetMessages();
}
