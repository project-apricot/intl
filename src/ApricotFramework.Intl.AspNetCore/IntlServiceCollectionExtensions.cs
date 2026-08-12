using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// Registers intl services on an <see cref="IServiceCollection"/>.
/// </summary>
public static class IntlServiceCollectionExtensions
{
    /// <summary>
    /// Adds the intl services: settings bound from the <c>Intl</c> configuration section, an
    /// <see cref="ILocaleAccessor"/>, an <see cref="ITranslationStore"/> and an
    /// <see cref="IIntlService"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The configuration to bind settings from.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <remarks>
    /// The three services are registered with <c>TryAdd</c>, so this call never replaces one already
    /// registered and calling it twice is harmless. Registration order is not significant: the
    /// container resolves the last registration for a single service, so an override registered after
    /// this call also wins, and translation sources are collected when the store is first constructed
    /// rather than when this method runs.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.
    /// </exception>
    public static IServiceCollection AddIntlCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<IntlSettings>(configuration.GetSection(IntlSettings.DefaultSettingsKey));
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ILocaleAccessor, DefaultLocaleAccessor>();
        services.TryAddSingleton<ITranslationStore, InMemoryTranslationStore>();
        services.TryAddSingleton<IIntlService, DefaultIntlService>();

        return services;
    }

    /// <summary>
    /// Adds a custom locale accessor.
    /// </summary>
    /// <typeparam name="TAccessor">The locale accessor type.</typeparam>
    /// <param name="services">The services collection.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddLocaleAccessor<TAccessor>(this IServiceCollection services)
        where TAccessor : class, ILocaleAccessor
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILocaleAccessor, TAccessor>();

        return services;
    }

    /// <summary>
    /// Adds a custom locale accessor instance.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="localeAccessor">The locale accessor.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="localeAccessor"/> is null.
    /// </exception>
    public static IServiceCollection AddLocaleAccessor(this IServiceCollection services, ILocaleAccessor localeAccessor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(localeAccessor);

        services.AddSingleton(localeAccessor);

        return services;
    }

    /// <summary>
    /// Adds a custom translation store.
    /// </summary>
    /// <typeparam name="TStore">The translation store type.</typeparam>
    /// <param name="services">The services collection.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddTranslationStore<TStore>(this IServiceCollection services)
        where TStore : class, ITranslationStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITranslationStore, TStore>();

        return services;
    }

    /// <summary>
    /// Adds a custom translation store instance.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="translationStore">The translation store.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="translationStore"/> is null.
    /// </exception>
    public static IServiceCollection AddTranslationStore(this IServiceCollection services, ITranslationStore translationStore)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(translationStore);

        services.AddSingleton(translationStore);

        return services;
    }

    /// <summary>
    /// Adds a custom translation source.
    /// </summary>
    /// <typeparam name="TSource">The translation source type.</typeparam>
    /// <param name="services">The services collection.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddTranslationSource<TSource>(this IServiceCollection services)
        where TSource : class, ITranslationSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITranslationSource, TSource>();

        return services;
    }

    /// <summary>
    /// Adds a custom translation source instance.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="translationSource">The translation source.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="translationSource"/> is null.
    /// </exception>
    public static IServiceCollection AddTranslationSource(this IServiceCollection services, ITranslationSource translationSource)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(translationSource);

        services.AddSingleton(translationSource);

        return services;
    }

    /// <summary>
    /// Adds a custom intl service.
    /// </summary>
    /// <typeparam name="TIntl">The intl service type.</typeparam>
    /// <param name="services">The services collection.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddIntlService<TIntl>(this IServiceCollection services)
        where TIntl : class, IIntlService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IIntlService, TIntl>();

        return services;
    }

    /// <summary>
    /// Adds a custom intl service instance.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="intlService">The intl service.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="intlService"/> is null.
    /// </exception>
    public static IServiceCollection AddIntlService(this IServiceCollection services, IIntlService intlService)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(intlService);

        services.AddSingleton(intlService);

        return services;
    }

    /// <summary>
    /// Points ASP.NET Core's own localization at intl, so <see cref="IStringLocalizer"/>,
    /// <see cref="IStringLocalizer{T}"/> and <see cref="IStringLocalizerFactory"/> resolve from your
    /// translation sources instead of <c>.resx</c> satellite assemblies.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <remarks>
    /// This is what makes Razor's <c>@Localizer["id"]</c> and localized DataAnnotations validation
    /// messages read from intl. Those consumers reach their localizer through
    /// <see cref="IStringLocalizerFactory"/>, so this registration is enough — but it must come
    /// <b>after</b> any <c>AddLocalization</c> call, whose <c>TryAdd</c> would otherwise have claimed
    /// the factory first. Registering here appends, and the container resolves the last registration.
    /// <para>
    /// Arguments reach these APIs positionally, so templates used through them take <c>{0}</c>,
    /// <c>{1}</c> placeholders rather than named ones. See <see cref="IntlStringLocalizer"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddIntlStringLocalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IStringLocalizerFactory, IntlStringLocalizerFactory>();
        services.AddSingleton<IStringLocalizer, IntlStringLocalizer>();
        services.AddSingleton(typeof(IStringLocalizer<>), typeof(IntlStringLocalizer<>));

        return services;
    }

    /// <summary>
    /// Adds a translation source read from a JSON stream of flat <c>id</c> to <c>template</c> pairs.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="locale">The locale the messages belong to.</param>
    /// <param name="supplier">
    /// Supplies the JSON stream. Invoked exactly once, during this call, and disposed before it
    /// returns.
    /// </param>
    /// <returns>The same services collection, so calls can be chained.</returns>
    /// <remarks>
    /// The stream is read eagerly here rather than when the source is resolved, so malformed JSON
    /// fails at startup. The document must be a flat object of string values; nested objects are not
    /// supported.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/>, <paramref name="locale"/> or
    /// <paramref name="supplier"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JSON document is null rather than an object of messages.
    /// </exception>
    /// <exception cref="JsonException">Thrown when the stream does not contain valid JSON.</exception>
    public static IServiceCollection AddJsonTranslationSource(this IServiceCollection services, string locale, Func<Stream> supplier)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(supplier);

        // take the stream from the supplier exactly once, and own its lifetime
        using var stream = supplier.Invoke();

        var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"The JSON translation source for locale '{locale}' is null rather than an object of messages.");

        return services.AddTranslationSource(new StaticTranslationSource(locale, messages));
    }
}
