namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// The intl settings, bound from configuration.
/// </summary>
public class IntlSettings
{
    /// <summary>
    /// The configuration section these settings are bound from by default.
    /// </summary>
    public const string DefaultSettingsKey = "Intl";

    /// <summary>
    /// The locale to fall back to when no locale can be resolved for a request, or when a message
    /// is missing from the requested locale. When unset, <c>en-US</c> is used.
    /// </summary>
    public string? FallbackLocale { get; set; }
}
