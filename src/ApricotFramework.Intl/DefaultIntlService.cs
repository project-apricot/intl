using System.Text;

namespace ApricotFramework.Intl;

/// <summary>
/// The default <see cref="IIntlService"/>: resolves a template from an <see cref="ITranslationStore"/>
/// and substitutes named placeholders into it.
/// </summary>
/// <remarks>
/// A template is looked for in this order: the requested locale, then progressively less specific
/// forms of it, then the fallback locale, then progressively less specific forms of that. So a
/// request for <c>hy-AM</c> with a fallback of <c>en-US</c> probes <c>hy-AM</c>, <c>hy</c>,
/// <c>en-US</c>, <c>en</c> — meaning a matching language always beats the fallback language.
/// Duplicates are dropped, so a store never sees the same locale twice in one call.
/// </remarks>
public class DefaultIntlService : IIntlService
{
    /// <summary>
    /// The translation store.
    /// </summary>
    private readonly ITranslationStore translationStore;

    /// <summary>
    /// The locale accessor.
    /// </summary>
    private readonly ILocaleAccessor localeAccessor;

    /// <summary>
    /// Creates a new instance of the default intl service.
    /// </summary>
    /// <param name="translationStore">The translation store.</param>
    /// <param name="localeAccessor">The locale accessor.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="translationStore"/> or <paramref name="localeAccessor"/> is null.
    /// </exception>
    public DefaultIntlService(ITranslationStore translationStore, ILocaleAccessor localeAccessor)
    {
        ArgumentNullException.ThrowIfNull(translationStore);
        ArgumentNullException.ThrowIfNull(localeAccessor);

        this.translationStore = translationStore;
        this.localeAccessor = localeAccessor;
    }

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
    /// or <paramref name="id"/> when no fallback was supplied.
    /// </returns>
    /// <remarks>
    /// The template is scanned once. Substituted values are never rescanned, so a value containing
    /// <c>{name}</c> is inserted literally, and the result does not depend on the order in which
    /// <paramref name="args"/> enumerates.
    /// </remarks>
    public virtual string Format(string id, string? locale, IDictionary<string, object?>? args = null, string? fallback = null)
    {
        // nothing translated degrades to the caller's fallback and only then to the id itself
        return this.FormatOrNull(id, locale, args) ?? fallback ?? id;
    }

    /// <summary>
    /// Finds the most specific template available for the id, widening the locale until something
    /// matches.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="requestedLocale">The locale asked for.</param>
    /// <param name="fallbackLocale">The locale to fall back to.</param>
    /// <returns>The template, or null when no locale in the search order has one.</returns>
    private string? ResolveTemplate(string id, string requestedLocale, string fallbackLocale)
    {
        // the requested locale itself is the overwhelmingly common hit: one lookup, nothing allocated
        var template = this.translationStore.GetTemplate(id, requestedLocale);

        if (template is not null)
        {
            return template;
        }

        // only on a miss is the wider search order worth building
        var candidates = BuildLocaleChain(requestedLocale, fallbackLocale);

        // index 0 is the requested locale, already tried above
        for (var index = 1; index < candidates.Count; index++)
        {
            template = this.translationStore.GetTemplate(id, candidates[index]);

            if (template is not null)
            {
                return template;
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the locale search order: the requested locale and progressively less specific forms of
    /// it, then the fallback locale and progressively less specific forms of that.
    /// </summary>
    /// <param name="requestedLocale">The locale asked for.</param>
    /// <param name="fallbackLocale">The locale to fall back to.</param>
    /// <returns>The locales to consult, most specific first, without duplicates.</returns>
    private static List<string> BuildLocaleChain(string requestedLocale, string fallbackLocale)
    {
        var chain = new List<string>();

        // this order is the contract - it decides which language a caller is served - so equivalents
        // are dropped by appending explicitly
        foreach (var candidate in LocaleWithParents(requestedLocale).Concat(LocaleWithParents(fallbackLocale)))
        {
            // compared case-insensitively, matching how the default store keys its locales
            if (!chain.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                chain.Add(candidate);
            }
        }

        return chain;
    }

    /// <summary>
    /// Enumerates a locale, then each progressively less specific form of it.
    /// </summary>
    /// <param name="locale">The locale to expand.</param>
    /// <returns>
    /// The locale itself first, then each form with one more trailing subtag removed:
    /// <c>zh-Hant-TW</c> yields <c>zh-Hant-TW</c>, <c>zh-Hant</c>, <c>zh</c>. A locale with no subtag
    /// separator yields only itself.
    /// </returns>
    private static IEnumerable<string> LocaleWithParents(string locale)
    {
        yield return locale;

        // every form is sliced from the original, so nothing is rescanned and `locale` is only read
        for (var index = locale.LastIndexOf('-'); index > 0; index = locale.LastIndexOf('-', index - 1))
        {
            yield return locale[..index];
        }
    }

    /// <summary>
    /// Replaces every <c>{name}</c> placeholder in the template with its argument value, in a single
    /// left-to-right pass.
    /// </summary>
    /// <param name="template">The message template.</param>
    /// <param name="args">The values to substitute.</param>
    /// <returns>The template with placeholders substituted.</returns>
    /// <remarks>
    /// A placeholder is an opening brace, a run of characters containing neither brace, and a
    /// closing brace. Anything else is literal text: an unmatched brace, an unknown key, or a brace
    /// run that never closes all survive verbatim. Because the scan only ever moves forward past a
    /// substituted value, that value is never itself treated as a template.
    /// </remarks>
    private static string Substitute(string template, IDictionary<string, object?> args)
    {
        var first = template.IndexOf('{');

        // no placeholder anywhere, so there is nothing to build
        if (first < 0)
        {
            return template;
        }

        var result = new StringBuilder(template.Length);
        result.Append(template, 0, first);

        var index = first;

        while (index < template.Length)
        {
            if (template[index] != '{')
            {
                result.Append(template[index]);
                index++;
                continue;
            }

            // look for the closing brace, giving up if another opening brace comes first
            var close = -1;

            for (var scan = index + 1; scan < template.Length; scan++)
            {
                if (template[scan] == '}')
                {
                    close = scan;
                    break;
                }

                if (template[scan] == '{')
                {
                    break;
                }
            }

            // not a placeholder after all, so the brace is literal
            if (close < 0)
            {
                result.Append(template[index]);
                index++;
                continue;
            }

            var key = template[(index + 1)..close];

            if (args.TryGetValue(key, out var value))
            {
                result.Append(value?.ToString() ?? string.Empty);
            }
            else
            {
                // an unknown key survives verbatim, braces included
                result.Append(template, index, close - index + 1);
            }

            // resume after the placeholder, never inside what replaced it
            index = close + 1;
        }

        return result.ToString();
    }

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
    /// or <paramref name="id"/> when no fallback was supplied.
    /// </returns>
    public virtual string Format(string id, IDictionary<string, object?>? args = null, string? fallback = null)
    {
        return this.Format(id, this.GetCurrentLocale(), args, fallback);
    }

    /// <summary>
    /// Formats the message with the given id, reporting a miss instead of degrading to a fallback.
    /// </summary>
    /// <param name="id">The message id.</param>
    /// <param name="locale">
    /// The target locale. When null or whitespace, the fallback locale is used.
    /// </param>
    /// <param name="args">
    /// Values substituted into <c>{name}</c> placeholders, or null when the message takes none.
    /// </param>
    /// <returns>The formatted message, or null when no translation was found.</returns>
    public virtual string? FormatOrNull(string id, string? locale, IDictionary<string, object?>? args = null)
    {
        // the fallback locale stands in when no locale was asked for
        var fallbackLocale = this.localeAccessor.GetFallbackLocale();
        var requestedLocale = string.IsNullOrWhiteSpace(locale) ? fallbackLocale : locale;

        var template = this.ResolveTemplate(id, requestedLocale, fallbackLocale);

        if (template is null)
        {
            return null;
        }

        if (args is null || args.Count == 0)
        {
            return template;
        }

        return Substitute(template, args);
    }

    /// <summary>
    /// Gets the locale currently in effect.
    /// </summary>
    /// <returns>
    /// The ambient locale, or the fallback locale when no ambient locale is available.
    /// </returns>
    public virtual string GetCurrentLocale()
    {
        var locale = this.localeAccessor.GetLocale();

        return string.IsNullOrWhiteSpace(locale) ? this.localeAccessor.GetFallbackLocale() : locale;
    }

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
    public virtual IReadOnlyDictionary<string, string> GetAllTemplates(string? locale, bool includeFallbacks = true)
    {
        var fallbackLocale = this.localeAccessor.GetFallbackLocale();
        var requestedLocale = string.IsNullOrWhiteSpace(locale) ? fallbackLocale : locale;

        if (!includeFallbacks)
        {
            return this.translationStore.GetAllTemplates(requestedLocale);
        }

        var merged = new Dictionary<string, string>(StringComparer.Ordinal);

        // most specific locale first, so nearer entries win and later ones only fill gaps - the same
        // precedence ResolveTemplate applies per id
        foreach (var candidate in BuildLocaleChain(requestedLocale, fallbackLocale))
        {
            foreach (var template in this.translationStore.GetAllTemplates(candidate))
            {
                merged.TryAdd(template.Key, template.Value);
            }
        }

        return merged;
    }
}
