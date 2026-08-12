namespace ApricotFramework.Intl;

/// <summary>
/// An <see cref="ITranslationSource"/> over a fixed set of message templates supplied up front.
/// </summary>
public class StaticTranslationSource : ITranslationSource
{
    /// <summary>
    /// The locale these messages belong to.
    /// </summary>
    private readonly string locale;

    /// <summary>
    /// The message templates, copied at construction.
    /// </summary>
    private readonly Dictionary<string, string> messages;

    /// <summary>
    /// Creates a new instance of the static translation source.
    /// </summary>
    /// <param name="locale">The locale these messages belong to.</param>
    /// <param name="messages">
    /// The message templates, keyed by message id. Copied, so later changes to the caller's
    /// dictionary do not affect this source.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="locale"/> or <paramref name="messages"/> is null.
    /// </exception>
    public StaticTranslationSource(string locale, IDictionary<string, string> messages)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(messages);

        this.locale = locale;
        this.messages = new Dictionary<string, string>(messages);
    }

    /// <summary>
    /// Gets the locale these messages belong to.
    /// </summary>
    /// <returns>The locale name.</returns>
    public string GetLocale()
    {
        return this.locale;
    }

    /// <summary>
    /// Gets the message templates, keyed by message id.
    /// </summary>
    /// <returns>The message templates.</returns>
    public IReadOnlyDictionary<string, string> GetMessages()
    {
        return this.messages;
    }
}
