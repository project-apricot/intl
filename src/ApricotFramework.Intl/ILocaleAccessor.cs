namespace ApricotFramework.Intl;

/// <summary>
/// Supplies the locale in effect, according to the hosting application's own logic.
/// </summary>
public interface ILocaleAccessor
{
    /// <summary>
    /// Gets the ambient locale.
    /// </summary>
    /// <returns>
    /// The ambient locale, or null when the application cannot determine one — for example
    /// outside of a request. Callers are expected to fall back to <see cref="GetFallbackLocale"/>.
    /// </returns>
    string? GetLocale();

    /// <summary>
    /// Gets the locale to use when no ambient locale is available, or when a message is missing
    /// from the requested locale.
    /// </summary>
    /// <returns>The fallback locale. Never null.</returns>
    string GetFallbackLocale();
}
