namespace ApricotFramework.Intl;

/// <summary>
/// Looks up message templates by id and locale.
/// </summary>
public interface ITranslationStore
{
    /// <summary>
    /// Gets the message template for an id in a locale.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="locale">The locale name.</param>
    /// <returns>
    /// The message template, or null when the locale or the id is not known to this store.
    /// </returns>
    string? GetTemplate(string id, string locale);

    /// <summary>
    /// Gets every message template known for a locale.
    /// </summary>
    /// <param name="locale">The locale name.</param>
    /// <returns>
    /// The templates keyed by message id, empty when the locale is not known to this store. Matching
    /// is exact, on the same terms as <see cref="GetTemplate"/>: no widening to less specific locales
    /// and no fallback locale.
    /// </returns>
    IReadOnlyDictionary<string, string> GetAllTemplates(string locale);
}
