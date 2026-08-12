using System.Globalization;
using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// Adapts <see cref="IIntlService"/> to ASP.NET Core's <see cref="IStringLocalizer"/>, so machinery
/// that localizes through the framework — Razor's <c>IViewLocalizer</c>, DataAnnotations validation
/// messages, anything taking <see cref="IStringLocalizer{T}"/> — reads from your translation sources
/// instead of <c>.resx</c> satellite assemblies.
/// </summary>
/// <remarks>
/// Two things differ from a <c>.resx</c>-backed localizer, both deliberate:
/// <list type="bullet">
/// <item>
/// The indexer takes positional arguments, so they are substituted into <c>{0}</c>, <c>{1}</c> and so
/// on — the shape DataAnnotations and <c>.resx</c> resources already use. Named <c>{placeholder}</c>
/// templates are still available through <see cref="IIntlService"/> directly; they simply cannot be
/// addressed through an interface that only carries an <see cref="object"/> array.
/// </item>
/// <item>
/// The locale comes from <see cref="IIntlService.GetCurrentLocale"/> rather than
/// <see cref="CultureInfo.CurrentUICulture"/>, so this and direct <c>IIntlService</c> calls can never
/// disagree within a request. With <c>UseRequestLocalization</c> registered the two agree anyway,
/// since <see cref="DefaultLocaleAccessor"/> reads the same request culture that middleware sets.
/// </item>
/// </list>
/// </remarks>
public class IntlStringLocalizer : IStringLocalizer
{
    /// <summary>
    /// The intl service.
    /// </summary>
    private readonly IIntlService intl;

    /// <summary>
    /// Creates a new instance of the localizer.
    /// </summary>
    /// <param name="intl">The intl service.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="intl"/> is null.</exception>
    public IntlStringLocalizer(IIntlService intl)
    {
        ArgumentNullException.ThrowIfNull(intl);

        this.intl = intl;
    }

    /// <summary>
    /// Gets the message with the given id, in the locale currently in effect.
    /// </summary>
    /// <param name="name">The message id.</param>
    /// <returns>
    /// The message, or one carrying the id as its value and
    /// <see cref="LocalizedString.ResourceNotFound"/> set when there is no translation.
    /// </returns>
    public virtual LocalizedString this[string name] => this.Localize(name, args: null);

    /// <summary>
    /// Gets the message with the given id, substituting positional arguments into <c>{0}</c>,
    /// <c>{1}</c> and so on.
    /// </summary>
    /// <param name="name">The message id.</param>
    /// <param name="arguments">The values to substitute, addressed by position.</param>
    /// <returns>
    /// The message, or one carrying the id as its value and
    /// <see cref="LocalizedString.ResourceNotFound"/> set when there is no translation.
    /// </returns>
    public virtual LocalizedString this[string name, params object[] arguments] => this.Localize(name, ToPositionalArguments(arguments));

    /// <summary>
    /// Gets every message available in the locale currently in effect.
    /// </summary>
    /// <param name="includeParentCultures">
    /// When true, messages missing from the current locale are filled in from the rest of the
    /// resolution order. When false, only messages defined for the locale itself are returned.
    /// </param>
    /// <returns>
    /// The messages, with placeholders left unsubstituted — the same as a <c>.resx</c>-backed
    /// localizer returns.
    /// </returns>
    public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var locale = this.intl.GetCurrentLocale();

        return this.intl.GetAllTemplates(locale, includeFallbacks: includeParentCultures)
            .Select(template => new LocalizedString(template.Key, template.Value, resourceNotFound: false, searchedLocation: locale))
            .ToList();
    }

    /// <summary>
    /// Resolves a message and wraps it, recording whether it was actually found.
    /// </summary>
    /// <param name="name">The message id.</param>
    /// <param name="args">The values to substitute, or null when the message takes none.</param>
    /// <returns>The resolved message.</returns>
    private LocalizedString Localize(string name, IDictionary<string, object?>? args)
    {
        var locale = this.intl.GetCurrentLocale();

        // FormatOrNull rather than Format: the contract needs a miss reported, not papered over
        var value = this.intl.FormatOrNull(name, locale, args);

        return new LocalizedString(name, value ?? name, resourceNotFound: value is null, searchedLocation: locale);
    }

    /// <summary>
    /// Keys positional arguments by their index, so they land on <c>{0}</c>, <c>{1}</c> and so on.
    /// </summary>
    /// <param name="arguments">The values to substitute.</param>
    /// <returns>The values keyed by index, or null when there are none.</returns>
    private static Dictionary<string, object?>? ToPositionalArguments(object[]? arguments)
    {
        if (arguments is null || arguments.Length == 0)
        {
            return null;
        }

        var args = new Dictionary<string, object?>(arguments.Length, StringComparer.Ordinal);

        for (var index = 0; index < arguments.Length; index++)
        {
            args[index.ToString(CultureInfo.InvariantCulture)] = arguments[index];
        }

        return args;
    }
}
