using System.Collections.ObjectModel;

namespace ApricotFramework.Intl;

/// <summary>
/// An <see cref="ITranslationStore"/> that flattens every <see cref="ITranslationSource"/> into
/// memory once, at construction.
/// </summary>
/// <remarks>
/// Locale names are matched case-insensitively, so <c>en-us</c> and <c>en-US</c> are the same key,
/// and sources declaring both merge into one. Matching is otherwise exact: this store does not walk
/// from <c>en-GB</c> to <c>en</c>. That widening is the caller's concern, and
/// <see cref="DefaultIntlService"/> does it by probing progressively less specific locales.
/// Message ids remain case-sensitive.
/// </remarks>
public class InMemoryTranslationStore : ITranslationStore
{
    /// <summary>
    /// Every message template, keyed by locale and then by message id.
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, string>> allMessages;

    /// <summary>
    /// Creates a new instance of the in-memory translation store.
    /// </summary>
    /// <param name="sources">
    /// The translation sources. Where several sources declare the same locale and message id, the
    /// last one enumerated wins.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null.</exception>
    public InMemoryTranslationStore(IEnumerable<ITranslationSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        // locale keys are case-insensitive; the inner id keys stay ordinal
        this.allMessages = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var locale = source.GetLocale();

            // start a bucket the first time we see this locale
            if (!this.allMessages.TryGetValue(locale, out var localeMessages))
            {
                localeMessages = [];
                this.allMessages[locale] = localeMessages;
            }

            // later sources overwrite earlier ones for the same id
            foreach (var message in source.GetMessages())
            {
                localeMessages[message.Key] = message.Value;
            }
        }
    }

    /// <summary>
    /// Gets the message template for an id in a locale.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="locale">The locale name.</param>
    /// <returns>
    /// The message template, or null when the locale or the id is not known to this store.
    /// </returns>
    public string? GetTemplate(string id, string locale)
    {
        if (!this.allMessages.TryGetValue(locale, out var templates))
        {
            return null;
        }

        return templates.TryGetValue(id, out var template) ? template : null;
    }

    /// <summary>
    /// Gets every message template known for a locale.
    /// </summary>
    /// <param name="locale">The locale name.</param>
    /// <returns>
    /// The templates keyed by message id, empty when the locale is not known to this store.
    /// </returns>
    public IReadOnlyDictionary<string, string> GetAllTemplates(string locale)
    {
        // wrapped rather than returned directly, so a caller cannot cast back and mutate the store
        return this.allMessages.TryGetValue(locale, out var templates)
            ? new ReadOnlyDictionary<string, string>(templates)
            : ReadOnlyDictionary<string, string>.Empty;
    }
}
