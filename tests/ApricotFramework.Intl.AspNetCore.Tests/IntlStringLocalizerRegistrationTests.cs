using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore.Tests;

public class IntlStringLocalizerRegistrationTests
{
    private sealed class SomeType;

    private sealed class AnotherType;

    private static ServiceProvider Provider(params (string Key, string Value)[] configuration)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

        return new ServiceCollection()
            .AddIntlCore(config)
            .AddTranslationSource(new StaticTranslationSource("en-US", new Dictionary<string, string>
            {
                ["messages.hello"] = "Hello",
                ["messages.welcome"] = "Welcome, {0}!",
            }))
            .AddIntlStringLocalization()
            .BuildServiceProvider();
    }

    [Fact]
    public void AddIntlStringLocalization_RegistersTheLocalizer()
    {
        var localizer = Provider().GetRequiredService<IStringLocalizer>();

        Assert.IsType<IntlStringLocalizer>(localizer);
        Assert.Equal("Hello", localizer["messages.hello"].Value);
    }

    [Fact]
    public void AddIntlStringLocalization_RegistersTheFactory()
    {
        var factory = Provider().GetRequiredService<IStringLocalizerFactory>();

        Assert.IsType<IntlStringLocalizerFactory>(factory);
        Assert.Equal("Hello", factory.Create(typeof(SomeType))["messages.hello"].Value);
        Assert.Equal("Hello", factory.Create("Base", "Location")["messages.hello"].Value);
    }

    [Fact]
    public void AddIntlStringLocalization_RegistersTheGenericLocalizer()
    {
        var localizer = Provider().GetRequiredService<IStringLocalizer<SomeType>>();

        Assert.Equal("Hello", localizer["messages.hello"].Value);
    }

    // The documented consequence of a flat id namespace: the type argument selects nothing.
    [Fact]
    public void GenericLocalizer_ResolvesTheSameMessage_ForAnyTypeArgument()
    {
        var provider = Provider();

        Assert.Equal(
            provider.GetRequiredService<IStringLocalizer<SomeType>>()["messages.hello"].Value,
            provider.GetRequiredService<IStringLocalizer<AnotherType>>()["messages.hello"].Value);
    }

    // AddLocalization uses TryAdd for the factory, so ours has to come after it to win. This is the
    // ordering that makes Razor and DataAnnotations read from intl rather than .resx.
    [Fact]
    public void AddIntlStringLocalization_OverridesAddLocalization_WhenCalledAfterIt()
    {
        var config = new ConfigurationBuilder().Build();

        var provider = new ServiceCollection()
            .AddIntlCore(config)
            .AddLocalization()
            .AddIntlStringLocalization()
            .BuildServiceProvider();

        Assert.IsType<IntlStringLocalizerFactory>(provider.GetRequiredService<IStringLocalizerFactory>());
    }

    [Fact]
    public void Localizer_SubstitutesPositionalArguments_ThroughTheContainer()
    {
        var localizer = Provider().GetRequiredService<IStringLocalizer>();

        Assert.Equal("Welcome, Joe!", localizer["messages.welcome", "Joe"].Value);
    }

    [Fact]
    public void Localizer_HonoursTheConfiguredFallbackLocale()
    {
        var localizer = Provider(("Intl:FallbackLocale", "en-US")).GetRequiredService<IStringLocalizer>();

        // no HttpContext, so the ambient locale is null and the fallback answers
        Assert.Equal("Hello", localizer["messages.hello"].Value);
    }

    [Fact]
    public void AddIntlStringLocalization_Throws_ForNullServices()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddIntlStringLocalization());
    }
}
