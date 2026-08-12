using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// Resolves the locale from the current HTTP request: first from the request culture established by
/// the localization middleware, then from a <c>locale</c> query-string value.
/// </summary>
public class DefaultLocaleAccessor : ILocaleAccessor
{
    /// <summary>
    /// The locale used when nothing is configured.
    /// </summary>
    private const string DefaultFallbackLocale = "en-US";

    /// <summary>
    /// The query-string key consulted when the request culture is unavailable.
    /// </summary>
    private const string LocaleQueryKey = "locale";

    /// <summary>
    /// The intl settings.
    /// </summary>
    private readonly IOptionsMonitor<IntlSettings> settings;

    /// <summary>
    /// The http context accessor.
    /// </summary>
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Creates a new instance of the default locale accessor.
    /// </summary>
    /// <param name="settings">The intl settings.</param>
    /// <param name="httpContextAccessor">The http context accessor.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings"/> or <paramref name="httpContextAccessor"/> is null.
    /// </exception>
    public DefaultLocaleAccessor(IOptionsMonitor<IntlSettings> settings, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        this.settings = settings;
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the locale for the current request.
    /// </summary>
    /// <returns>
    /// The request culture name when the localization middleware has established one, otherwise the
    /// <c>locale</c> query-string value, otherwise null — including when there is no active request.
    /// </returns>
    public virtual string? GetLocale()
    {
        var context = this.httpContextAccessor.HttpContext;

        if (context is null)
        {
            return null;
        }

        // the localization middleware publishes the negotiated culture as a feature. Note the name
        // is empty rather than null under the invariant culture, so blank counts as "not resolved".
        var cultureName = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;

        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            return cultureName;
        }

        // fall back to an explicit query-string override
        if (context.Request.Query.TryGetValue(LocaleQueryKey, out var locales) && locales.Count > 0)
        {
            return locales[0];
        }

        return null;
    }

    /// <summary>
    /// Gets the configured fallback locale.
    /// </summary>
    /// <returns>
    /// <see cref="IntlSettings.FallbackLocale"/> when configured, otherwise <c>en-US</c>.
    /// </returns>
    public virtual string GetFallbackLocale()
    {
        var configured = this.settings.CurrentValue.FallbackLocale;

        return string.IsNullOrWhiteSpace(configured) ? DefaultFallbackLocale : configured;
    }
}
