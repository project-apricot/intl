namespace ApricotFramework.Intl.Tests;

public class DefaultIntlServiceTests
{
    private const string Fallback = "en-US";

    private sealed class StubLocaleAccessor(string? locale, string fallbackLocale = Fallback) : ILocaleAccessor
    {
        public string? GetLocale() => locale;

        public string GetFallbackLocale() => fallbackLocale;
    }

    private static DefaultIntlService Service(string? ambientLocale, params (string Locale, string Id, string Template)[] messages)
    {
        var sources = messages
            .GroupBy(m => m.Locale)
            .Select(g => new StaticTranslationSource(g.Key, g.ToDictionary(m => m.Id, m => m.Template)))
            .Cast<ITranslationSource>()
            .ToList();

        return new DefaultIntlService(new InMemoryTranslationStore(sources), new StubLocaleAccessor(ambientLocale));
    }

    [Fact]
    public void Format_ReturnsTemplate_ForExplicitLocale()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.Format("messages.hello", "hy-AM"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_UsesFallbackLocale_ForBlankLocale(string? locale)
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"));

        Assert.Equal("Hello", service.Format("messages.hello", locale));
    }

    [Fact]
    public void Format_FallsBackToFallbackLocale_WhenMissingInRequestedLocale()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"), ("hy-AM", "messages.other", "Այլ"));

        Assert.Equal("Hello", service.Format("messages.hello", "hy-AM"));
    }

    [Fact]
    public void Format_PrefersRequestedLocale_OverFallbackLocale()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"), ("hy-AM", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.Format("messages.hello", "hy-AM"));
    }

    // Golden behaviour. The search order is: requested locale, progressively less specific forms of
    // it, then the fallback locale and progressively less specific forms of that. The tests below pin
    // each step. Changing the order changes which language a consumer is served, so it needs a major.

    [Fact]
    public void Format_FallsBackToParentOfRequestedLocale()
    {
        var service = Service(null, ("hy", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.Format("messages.hello", "hy-AM"));
    }

    // This is the case that used to silently serve the wrong language: hy is registered, hy-AM is
    // requested, and the old exact-match-then-fallback chain skipped straight past hy to English.
    [Fact]
    public void Format_PrefersParentOfRequestedLocale_OverFallbackLocale()
    {
        var service = Service(null, ("hy", "messages.hello", "Բարև"), (Fallback, "messages.hello", "Hello"));

        Assert.Equal("Բարև", service.Format("messages.hello", "hy-AM"));
    }

    [Fact]
    public void Format_PrefersExactRequestedLocale_OverItsParent()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև (AM)"), ("hy", "messages.hello", "Բարև"));

        Assert.Equal("Բարև (AM)", service.Format("messages.hello", "hy-AM"));
    }

    [Fact]
    public void Format_WalksMoreThanOneLevel()
    {
        var service = Service(null, ("zh", "messages.hello", "你好"));

        Assert.Equal("你好", service.Format("messages.hello", "zh-Hant-TW"));
    }

    [Fact]
    public void Format_PrefersNearerAncestor_WhenSeveralMatch()
    {
        var service = Service(null, ("zh-Hant", "messages.hello", "你好 (Hant)"), ("zh", "messages.hello", "你好"));

        Assert.Equal("你好 (Hant)", service.Format("messages.hello", "zh-Hant-TW"));
    }

    [Fact]
    public void Format_FallsBackToParentOfFallbackLocale()
    {
        var service = Service(null, ("en", "messages.hello", "Hello"));

        // fr-FR and fr have nothing, the en-US fallback has nothing, so en answers
        Assert.Equal("Hello", service.Format("messages.hello", "fr-FR"));
    }

    [Fact]
    public void Format_MatchesRequestedLocale_IgnoringCase()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.Format("messages.hello", "HY-am"));
    }

    [Fact]
    public void Format_ReturnsId_WhenNoLocaleInTheChainHasTheMessage()
    {
        var service = Service(null, ("hy", "messages.other", "Այլ"));

        Assert.Equal("messages.hello", service.Format("messages.hello", "hy-AM"));
    }

    [Fact]
    public void Format_ReturnsSuppliedFallback_WhenTranslationMissing()
    {
        var service = Service(null);

        Assert.Equal("Hi there", service.Format("messages.hello", "en-US", args: null, fallback: "Hi there"));
    }

    [Fact]
    public void Format_ReturnsId_WhenTranslationMissingAndNoFallback()
    {
        var service = Service(null);

        Assert.Equal("messages.hello", service.Format("messages.hello", "en-US"));
    }

    [Fact]
    public void Format_ReturnsTemplateUnchanged_WhenNoArgs()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        Assert.Equal("Hello {name}", service.Format("messages.greet", Fallback));
    }

    [Fact]
    public void Format_ReturnsTemplateUnchanged_WhenArgsEmpty()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        Assert.Equal("Hello {name}", service.Format("messages.greet", Fallback, new Dictionary<string, object?>()));
    }

    [Fact]
    public void Format_SubstitutesNamedPlaceholder()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        var result = service.Format("messages.greet", Fallback, new Dictionary<string, object?> { ["name"] = "Joe" });

        Assert.Equal("Hello Joe", result);
    }

    [Fact]
    public void Format_SubstitutesEveryOccurrenceOfAPlaceholder()
    {
        var service = Service(null, (Fallback, "messages.twice", "{name} and {name}"));

        var result = service.Format("messages.twice", Fallback, new Dictionary<string, object?> { ["name"] = "X" });

        Assert.Equal("X and X", result);
    }

    [Fact]
    public void Format_SubstitutesMultiplePlaceholders()
    {
        var service = Service(null, (Fallback, "messages.pair", "{a}-{b}"));

        var result = service.Format("messages.pair", Fallback, new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 });

        Assert.Equal("1-2", result);
    }

    [Fact]
    public void Format_SubstitutesEmptyString_ForNullArgValue()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        var result = service.Format("messages.greet", Fallback, new Dictionary<string, object?> { ["name"] = null });

        Assert.Equal("Hello ", result);
    }

    [Fact]
    public void Format_LeavesPlaceholderLiteral_WhenArgNotSupplied()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        var result = service.Format("messages.greet", Fallback, new Dictionary<string, object?> { ["other"] = "X" });

        Assert.Equal("Hello {name}", result);
    }

    [Fact]
    public void Format_IgnoresArgsWithNoMatchingPlaceholder()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"));

        var result = service.Format("messages.hello", Fallback, new Dictionary<string, object?> { ["unused"] = "X" });

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Format_AmbientOverload_UsesCurrentLocale()
    {
        var service = Service("hy-AM", (Fallback, "messages.hello", "Hello"), ("hy-AM", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.Format("messages.hello"));
    }

    [Fact]
    public void Format_AmbientOverload_UsesFallbackLocale_WhenNoAmbientLocale()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"));

        Assert.Equal("Hello", service.Format("messages.hello"));
    }

    [Fact]
    public void Format_AmbientOverload_HonoursSuppliedFallback()
    {
        var service = Service(null);

        Assert.Equal("Hi there", service.Format("messages.hello", args: null, fallback: "Hi there"));
    }

    [Fact]
    public void GetCurrentLocale_ReturnsAmbientLocale()
    {
        Assert.Equal("hy-AM", Service("hy-AM").GetCurrentLocale());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCurrentLocale_ReturnsFallbackLocale_ForBlankAmbientLocale(string? ambient)
    {
        Assert.Equal(Fallback, Service(ambient).GetCurrentLocale());
    }

    /// <summary>
    /// Records the locales it is asked about, answering only for the one locale it was given.
    /// </summary>
    private sealed class RecordingTranslationStore(string? answersFor = null) : ITranslationStore
    {
        public List<string> Probed { get; } = [];

        public string? GetTemplate(string id, string locale)
        {
            this.Probed.Add(locale);

            return string.Equals(locale, answersFor, StringComparison.OrdinalIgnoreCase) ? "answer" : null;
        }

        public IReadOnlyDictionary<string, string> GetAllTemplates(string locale)
        {
            this.Probed.Add(locale);

            return string.Equals(locale, answersFor, StringComparison.OrdinalIgnoreCase)
                ? new Dictionary<string, string> { ["messages.hello"] = "answer" }
                : new Dictionary<string, string>();
        }
    }

    // Golden behaviour. The documented search order, and the promise that duplicates are dropped so a
    // store never sees the same locale twice - which matters when a lookup is a round trip.
    [Fact]
    public void Format_ProbesLocalesInOrder_WithoutRepeating()
    {
        var store = new RecordingTranslationStore();
        var service = new DefaultIntlService(store, new StubLocaleAccessor("en-GB", "en-US"));

        service.Format("messages.hello", "en-GB");

        // en is reached via en-GB, so the en-US chain must not offer it a second time
        Assert.Equal(["en-GB", "en", "en-US"], store.Probed);
    }

    // Pins the chain the parent walk produces, including malformed tags. The fallback is "xx" so it
    // cannot be confused with anything derived from the requested locale.
    [Theory]
    [InlineData("hy", new[] { "hy", "xx" })]
    [InlineData("hy-AM", new[] { "hy-AM", "hy", "xx" })]
    [InlineData("zh-Hant-TW", new[] { "zh-Hant-TW", "zh-Hant", "zh", "xx" })]
    // a trailing separator yields the stem, not an empty locale
    [InlineData("en-", new[] { "en-", "en", "xx" })]
    // a leading separator has no parent, so the walk stops rather than yielding an empty locale
    [InlineData("-en", new[] { "-en", "xx" })]
    public void Format_ProbesTheExpectedLocaleChain(string requested, string[] expected)
    {
        var store = new RecordingTranslationStore();
        var service = new DefaultIntlService(store, new StubLocaleAccessor(requested, "xx"));

        service.Format("messages.hello", requested);

        Assert.Equal(expected, store.Probed);
    }

    [Fact]
    public void Format_DoesNotRepeatTheLocale_WhenRequestedMatchesFallback()
    {
        var store = new RecordingTranslationStore();
        var service = new DefaultIntlService(store, new StubLocaleAccessor("en-US", "en-US"));

        service.Format("messages.hello", "en-US");

        Assert.Equal(["en-US", "en"], store.Probed);
    }

    [Fact]
    public void Format_ProbesOnlyOnce_WhenTheRequestedLocaleHasTheMessage()
    {
        var store = new RecordingTranslationStore(answersFor: "hy-AM");
        var service = new DefaultIntlService(store, new StubLocaleAccessor("hy-AM"));

        service.Format("messages.hello", "hy-AM");

        // a hit on the first probe must not build or walk the wider chain
        Assert.Equal("hy-AM", Assert.Single(store.Probed));
    }

    [Fact]
    public void FormatOrNull_ReturnsFormattedMessage_WhenFound()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        var result = service.FormatOrNull("messages.greet", Fallback, new Dictionary<string, object?> { ["name"] = "Joe" });

        Assert.Equal("Hello Joe", result);
    }

    // The whole point of FormatOrNull: Format cannot tell "translated to its own id" from "not found".
    [Fact]
    public void FormatOrNull_ReturnsNull_WhenNotFound()
    {
        var service = Service(null);

        Assert.Null(service.FormatOrNull("messages.hello", Fallback));
        Assert.Equal("messages.hello", service.Format("messages.hello", Fallback));
    }

    [Fact]
    public void FormatOrNull_DistinguishesAMessageWhoseValueEqualsItsId()
    {
        var service = Service(null, (Fallback, "messages.hello", "messages.hello"));

        // Format would return "messages.hello" either way; FormatOrNull proves it was a real hit
        Assert.Equal("messages.hello", service.FormatOrNull("messages.hello", Fallback));
        Assert.Null(service.FormatOrNull("messages.absent", Fallback));
    }

    [Fact]
    public void FormatOrNull_UsesTheSameLocaleChainAsFormat()
    {
        var service = Service(null, ("hy", "messages.hello", "Բարև"));

        Assert.Equal("Բարև", service.FormatOrNull("messages.hello", "hy-AM"));
    }

    [Fact]
    public void GetAllTemplates_ReturnsTemplatesForTheLocale()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"), ("hy-AM", "messages.bye", "Ցտեսություն"));

        var all = service.GetAllTemplates("hy-AM");

        Assert.Equal("Բարև", all["messages.hello"]);
        Assert.Equal("Ցտեսություն", all["messages.bye"]);
    }

    [Fact]
    public void GetAllTemplates_FillsGapsFromTheRestOfTheChain()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"), (Fallback, "messages.bye", "Bye"));

        var all = service.GetAllTemplates("hy-AM");

        Assert.Equal("Բարև", all["messages.hello"]);
        Assert.Equal("Bye", all["messages.bye"]);
    }

    [Fact]
    public void GetAllTemplates_PrefersTheNearerLocale_ForTheSameId()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"), ("hy", "messages.hello", "Բարև (hy)"), (Fallback, "messages.hello", "Hello"));

        Assert.Equal("Բարև", service.GetAllTemplates("hy-AM")["messages.hello"]);
    }

    [Fact]
    public void GetAllTemplates_ExcludesOtherLocales_WhenFallbacksNotWanted()
    {
        var service = Service(null, ("hy-AM", "messages.hello", "Բարև"), (Fallback, "messages.bye", "Bye"));

        var all = service.GetAllTemplates("hy-AM", includeFallbacks: false);

        Assert.Equal("Բարև", Assert.Single(all).Value);
    }

    [Fact]
    public void GetAllTemplates_UsesFallbackLocale_ForBlankLocale()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"));

        Assert.Equal("Hello", service.GetAllTemplates(null)["messages.hello"]);
    }

    [Fact]
    public void GetAllTemplates_IsEmpty_ForUnknownLocaleWithoutFallbacks()
    {
        var service = Service(null, (Fallback, "messages.hello", "Hello"));

        Assert.Empty(service.GetAllTemplates("fr-FR", includeFallbacks: false));
    }

    // The contract that makes GetAllTemplates trustworthy: every entry it reports is exactly what
    // Format would resolve for that id, so the two can never disagree.
    [Fact]
    public void GetAllTemplates_AgreesWithFormat_ForEveryEntry()
    {
        var service = Service(
            null,
            ("hy-AM", "messages.hello", "Բարև"),
            ("hy", "messages.only-hy", "Միայն hy"),
            ("hy", "messages.hello", "Բարև (hy)"),
            (Fallback, "messages.bye", "Bye"),
            (Fallback, "messages.hello", "Hello"),
            ("en", "messages.only-en", "Only en"));

        var all = service.GetAllTemplates("hy-AM");

        Assert.NotEmpty(all);
        Assert.All(all, entry => Assert.Equal(entry.Value, service.Format(entry.Key, "hy-AM")));
    }

    [Fact]
    public void Constructor_Throws_ForNullTranslationStore()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultIntlService(null!, new StubLocaleAccessor(null)));
    }

    [Fact]
    public void Constructor_Throws_ForNullLocaleAccessor()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultIntlService(new InMemoryTranslationStore([]), null!));
    }

    [Fact]
    public void Format_DoesNotReexpandPlaceholdersInsideSubstitutedValues()
    {
        var service = Service(null, (Fallback, "messages.nested", "{a}"));

        var args = new Dictionary<string, object?>
        {
            ["a"] = "{b}",
            ["b"] = "INJECTED",
        };

        Assert.Equal("{b}", service.Format("messages.nested", Fallback, args));
    }

    [Fact]
    public void Format_IsIndependentOfArgumentOrder()
    {
        var service = Service(null, (Fallback, "messages.nested", "{a}/{b}"));

        var ascending = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["a"] = "{b}",
            ["b"] = "VALUE",
        };

        var descending = new SortedDictionary<string, object?>(
            Comparer<string>.Create((left, right) => string.CompareOrdinal(right, left)))
        {
            ["a"] = "{b}",
            ["b"] = "VALUE",
        };

        Assert.Equal("{b}/VALUE", service.Format("messages.nested", Fallback, ascending));
        Assert.Equal("{b}/VALUE", service.Format("messages.nested", Fallback, descending));
    }

    [Fact]
    public void Format_SubstitutesAdjacentPlaceholders()
    {
        var service = Service(null, (Fallback, "messages.pair", "{a}{b}"));

        var result = service.Format("messages.pair", Fallback, new Dictionary<string, object?> { ["a"] = "X", ["b"] = "Y" });

        Assert.Equal("XY", result);
    }

    [Theory]
    [InlineData("Hello {name", "Hello {name")]
    [InlineData("Hello name}", "Hello name}")]
    [InlineData("Hello {}", "Hello {}")]
    [InlineData("{outer{name}", "{outerJohn")]
    [InlineData("[{name}]", "[John]")]
    public void Format_TreatsMalformedPlaceholdersAsLiteralText(string template, string expected)
    {
        var service = Service(null, (Fallback, "messages.odd", template));

        var result = service.Format("messages.odd", Fallback, new Dictionary<string, object?> { ["name"] = "John" });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_PreservesBracesInSubstitutedValues_Exactly()
    {
        var service = Service(null, (Fallback, "messages.greet", "Hello {name}"));

        var result = service.Format("messages.greet", Fallback, new Dictionary<string, object?> { ["name"] = "{{weird}}" });

        Assert.Equal("Hello {{weird}}", result);
    }
}
