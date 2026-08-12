namespace ApricotFramework.Intl;

/// <summary>
/// Resolves and formats localized messages.
/// </summary>
public interface IIntlService
{
    /// <summary>
    /// Formats the message with the given id for an explicit locale.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="locale">
    /// The target locale. When null or whitespace, the fallback locale is used.
    /// </param>
    /// <param name="args">
    /// Values substituted into <c>{name}</c> placeholders, or null when the message takes none.
    /// </param>
    /// <param name="fallback">The message to return when no translation is found.</param>
    /// <returns>
    /// The formatted message; otherwise <paramref name="fallback"/> when no translation is found,
    /// or <paramref name="id"/> when no fallback was supplied. Never null.
    /// </returns>
    string Format(string id, string? locale, IDictionary<string, object?>? args = null, string? fallback = null);

    /// <summary>
    /// Formats the message with the given id for the current locale.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="args">
    /// Values substituted into <c>{name}</c> placeholders, or null when the message takes none.
    /// </param>
    /// <param name="fallback">The message to return when no translation is found.</param>
    /// <returns>
    /// The formatted message; otherwise <paramref name="fallback"/> when no translation is found,
    /// or <paramref name="id"/> when no fallback was supplied. Never null.
    /// </returns>
    /// <remarks>
    /// Calling this with an explicit null second argument (<c>Format("id", null)</c>) is ambiguous
    /// with the overload taking a locale and will not compile. Name the argument to disambiguate.
    /// </remarks>
    string Format(string id, IDictionary<string, object?>? args = null, string? fallback = null);

    /// <summary>
    /// Formats the message with the given id, reporting a miss instead of degrading to a fallback.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="locale">
    /// The target locale. When null or whitespace, the fallback locale is used. Pass
    /// <see cref="GetCurrentLocale"/> to format for the locale currently in effect.
    /// </param>
    /// <param name="args">
    /// Values substituted into <c>{name}</c> placeholders, or null when the message takes none.
    /// </param>
    /// <returns>The formatted message, or null when no translation was found.</returns>
    /// <remarks>
    /// The counterpart to <see cref="Format(string, string?, IDictionary{string, object?}?, string?)"/>,
    /// which cannot distinguish a message that resolved to its own id from one that was found. Use this
    /// when the difference matters — reporting untranslated ids, or populating a
    /// <c>resourceNotFound</c>-style flag.
    /// </remarks>
    string? FormatOrNull(string id, string? locale, IDictionary<string, object?>? args = null);

    /// <summary>
    /// Gets the locale currently in effect.
    /// </summary>
    /// <returns>
    /// The ambient locale, or the fallback locale when no ambient locale is available.
    /// </returns>
    string GetCurrentLocale();

    /// <summary>
    /// Gets every message template that would resolve for a locale.
    /// </summary>
    /// <param name="locale">
    /// The target locale. When null or whitespace, the fallback locale is used.
    /// </param>
    /// <param name="includeFallbacks">
    /// When true, messages missing from the locale are filled in from the rest of the resolution
    /// order. When false, only templates defined for the locale itself are returned.
    /// </param>
    /// <returns>The templates keyed by message id.</returns>
    /// <remarks>
    /// With <paramref name="includeFallbacks"/> true, the result matches what
    /// <see cref="Format(string, string?, IDictionary{string, object?}?, string?)"/> would resolve for
    /// each id, so it is the whole catalog a caller would see for that locale. Useful for handing a
    /// locale's messages to a client — serialising them for a browser, say — and for reporting
    /// coverage gaps between locales.
    /// </remarks>
    IReadOnlyDictionary<string, string> GetAllTemplates(string? locale, bool includeFallbacks = true);
}
