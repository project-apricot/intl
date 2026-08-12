using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore.Tests;

public class IntlStringLocalizerTests
{
    private sealed class StubLocaleAccessor(string? locale, string fallbackLocale = "en-US") : ILocaleAccessor
    {
        public string? GetLocale() => locale;

        public string GetFallbackLocale() => fallbackLocale;
    }

    private static IntlStringLocalizer Localizer(string? ambientLocale, params (string Locale, string Id, string Template)[] messages)
    {
        var sources = messages
            .GroupBy(m => m.Locale)
            .Select(g => new StaticTranslationSource(g.Key, g.ToDictionary(m => m.Id, m => m.Template)))
            .Cast<ITranslationSource>()
            .ToList();

        var intl = new DefaultIntlService(new InMemoryTranslationStore(sources), new StubLocaleAccessor(ambientLocale));

        return new IntlStringLocalizer(intl);
    }

    [Fact]
    public void Indexer_ReturnsTheTranslation_ForAKnownId()
    {
        var localizer = Localizer("hy-AM", ("hy-AM", "messages.hello", "Բարև"));

        var localized = localizer["messages.hello"];

        Assert.Equal("messages.hello", localized.Name);
        Assert.Equal("Բարև", localized.Value);
        Assert.False(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_ImplicitlyConvertsToItsValue()
    {
        var localizer = Localizer("hy-AM", ("hy-AM", "messages.hello", "Բարև"));

        // consumers write @Localizer["id"] and expect the string, not the wrapper
        string value = localizer["messages.hello"];

        Assert.Equal("Բարև", value);
    }

    // The reason FormatOrNull had to exist: Format alone returns the id on a miss, which is
    // indistinguishable from a message whose value happens to equal its id.
    [Fact]
    public void Indexer_ReportsResourceNotFound_ForAnUnknownId()
    {
        var localizer = Localizer("hy-AM");

        var localized = localizer["messages.absent"];

        Assert.True(localized.ResourceNotFound);
        Assert.Equal("messages.absent", localized.Name);
        Assert.Equal("messages.absent", localized.Value);
    }

    [Fact]
    public void Indexer_DoesNotReportResourceNotFound_WhenAValueEqualsItsId()
    {
        var localizer = Localizer("en-US", ("en-US", "messages.hello", "messages.hello"));

        Assert.False(localizer["messages.hello"].ResourceNotFound);
    }

    [Fact]
    public void Indexer_RecordsTheLocaleItSearched()
    {
        var localizer = Localizer("hy-AM", ("hy-AM", "messages.hello", "Բարև"));

        Assert.Equal("hy-AM", localizer["messages.hello"].SearchedLocation);
    }

    [Fact]
    public void Indexer_SubstitutesPositionalArguments()
    {
        var localizer = Localizer("en-US", ("en-US", "messages.welcome", "Welcome, {0}!"));

        Assert.Equal("Welcome, Joe!", localizer["messages.welcome", "Joe"].Value);
    }

    [Fact]
    public void Indexer_SubstitutesSeveralPositionalArguments()
    {
        var localizer = Localizer("en-US", ("en-US", "messages.range", "{0} to {1}"));

        Assert.Equal("1 to 9", localizer["messages.range", 1, 9].Value);
    }

    // The shape DataAnnotations uses: {0} is the display name of the member being validated.
    [Fact]
    public void Indexer_SupportsTheDataAnnotationsMessageShape()
    {
        var localizer = Localizer("en-US", ("en-US", "validation.required", "The {0} field is required."));

        Assert.Equal("The Email field is required.", localizer["validation.required", "Email"].Value);
    }

    [Fact]
    public void Indexer_ReportsResourceNotFound_ForAnUnknownIdWithArguments()
    {
        var localizer = Localizer("en-US");

        var localized = localizer["messages.absent", "Joe"];

        Assert.True(localized.ResourceNotFound);
        Assert.Equal("messages.absent", localized.Value);
    }

    [Fact]
    public void Indexer_ToleratesANullArgumentArray()
    {
        var localizer = Localizer("en-US", ("en-US", "messages.hello", "Hello"));

        Assert.Equal("Hello", localizer["messages.hello", null!].Value);
    }

    [Fact]
    public void Indexer_ResolvesThroughTheLocaleChain()
    {
        var localizer = Localizer("hy-AM", ("hy", "messages.hello", "Բարև"));

        // hy-AM has nothing, so the parent hy answers - the same chain Format uses
        Assert.Equal("Բարև", localizer["messages.hello"].Value);
    }

    [Fact]
    public void Indexer_UsesTheFallbackLocale_WhenNoAmbientLocale()
    {
        var localizer = Localizer(null, ("en-US", "messages.hello", "Hello"));

        Assert.Equal("Hello", localizer["messages.hello"].Value);
    }

    [Fact]
    public void GetAllStrings_ReturnsEveryMessage_WithPlaceholdersIntact()
    {
        var localizer = Localizer("en-US", ("en-US", "messages.hello", "Hello"), ("en-US", "messages.welcome", "Welcome, {0}!"));

        var all = localizer.GetAllStrings(includeParentCultures: true).ToList();

        Assert.Equal(2, all.Count);
        Assert.Equal("Welcome, {0}!", all.Single(s => s.Name == "messages.welcome").Value);
        Assert.All(all, s => Assert.False(s.ResourceNotFound));
    }

    [Fact]
    public void GetAllStrings_IncludesInheritedMessages_WhenParentCulturesWanted()
    {
        var localizer = Localizer("hy-AM", ("hy-AM", "messages.hello", "Բարև"), ("en-US", "messages.bye", "Bye"));

        var names = localizer.GetAllStrings(includeParentCultures: true).Select(s => s.Name).ToList();

        Assert.Contains("messages.hello", names);
        Assert.Contains("messages.bye", names);
    }

    [Fact]
    public void GetAllStrings_ExcludesInheritedMessages_WhenParentCulturesNotWanted()
    {
        var localizer = Localizer("hy-AM", ("hy-AM", "messages.hello", "Բարև"), ("en-US", "messages.bye", "Bye"));

        var names = localizer.GetAllStrings(includeParentCultures: false).Select(s => s.Name).ToList();

        Assert.Equal(["messages.hello"], names);
    }

    [Fact]
    public void Constructor_Throws_ForNullIntlService()
    {
        Assert.Throws<ArgumentNullException>(() => new IntlStringLocalizer(null!));
    }
}
