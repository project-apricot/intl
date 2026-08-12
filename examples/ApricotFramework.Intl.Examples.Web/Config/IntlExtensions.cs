using ApricotFramework.Intl.AspNetCore;

namespace ApricotFramework.Intl.Examples.Web.Config;

/// <summary>
/// Wires intl up for this application.
/// </summary>
public static class IntlExtensions
{
    /// <summary>
    /// Adds the intl services and this application's translation files.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The configuration reference.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    public static IServiceCollection AddIntl(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIntlCore(configuration);

        // the translation files are copied next to the built assembly
        var translationsBase = Path.Combine(AppContext.BaseDirectory, "Translations");

        // order relative to AddIntlCore does not matter
        services.AddJsonTranslationSource("en-US", () => File.OpenRead(Path.Combine(translationsBase, "all.en.json")));
        services.AddJsonTranslationSource("hy-AM", () => File.OpenRead(Path.Combine(translationsBase, "all.hy.json")));

        // point the framework's own IStringLocalizer at intl, so Razor and DataAnnotations would read
        // from these same files
        services.AddIntlStringLocalization();

        return services;
    }
}
