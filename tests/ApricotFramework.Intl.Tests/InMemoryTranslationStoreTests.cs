namespace ApricotFramework.Intl.Tests;

public class InMemoryTranslationStoreTests
{
    private static StaticTranslationSource Source(string locale, params (string Id, string Template)[] messages)
    {
        return new StaticTranslationSource(locale, messages.ToDictionary(m => m.Id, m => m.Template));
    }

    [Fact]
    public void GetTemplate_ReturnsTemplate_ForKnownIdAndLocale()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Equal("Hello", store.GetTemplate("messages.hello", "en-US"));
    }

    [Fact]
    public void GetTemplate_ReturnsNull_ForUnknownLocale()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Null(store.GetTemplate("messages.hello", "hy-AM"));
    }

    [Fact]
    public void GetTemplate_ReturnsNull_ForUnknownId()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Null(store.GetTemplate("messages.missing", "en-US"));
    }

    [Fact]
    public void GetTemplate_ReturnsNull_WhenNoSources()
    {
        var store = new InMemoryTranslationStore([]);

        Assert.Null(store.GetTemplate("messages.hello", "en-US"));
    }

    [Fact]
    public void Constructor_MergesSources_ForTheSameLocale()
    {
        var store = new InMemoryTranslationStore(
        [
            Source("en-US", ("messages.hello", "Hello")),
            Source("en-US", ("messages.bye", "Bye")),
        ]);

        Assert.Equal("Hello", store.GetTemplate("messages.hello", "en-US"));
        Assert.Equal("Bye", store.GetTemplate("messages.bye", "en-US"));
    }

    [Fact]
    public void Constructor_LastSourceWins_ForDuplicateId()
    {
        var store = new InMemoryTranslationStore(
        [
            Source("en-US", ("messages.hello", "First")),
            Source("en-US", ("messages.hello", "Second")),
        ]);

        Assert.Equal("Second", store.GetTemplate("messages.hello", "en-US"));
    }

    [Fact]
    public void Constructor_KeepsLocalesSeparate()
    {
        var store = new InMemoryTranslationStore(
        [
            Source("en-US", ("messages.hello", "Hello")),
            Source("hy-AM", ("messages.hello", "Բարև")),
        ]);

        Assert.Equal("Hello", store.GetTemplate("messages.hello", "en-US"));
        Assert.Equal("Բարև", store.GetTemplate("messages.hello", "hy-AM"));
    }

    [Fact]
    public void GetAllTemplates_ReturnsEveryTemplateForTheLocale()
    {
        var store = new InMemoryTranslationStore(
        [
            Source("en-US", ("messages.hello", "Hello"), ("messages.bye", "Bye")),
            Source("hy-AM", ("messages.hello", "Բարև")),
        ]);

        var all = store.GetAllTemplates("en-US");

        Assert.Equal(2, all.Count);
        Assert.Equal("Hello", all["messages.hello"]);
        Assert.Equal("Bye", all["messages.bye"]);
    }

    [Fact]
    public void GetAllTemplates_MatchesLocaleName_CaseInsensitively()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Equal("Hello", store.GetAllTemplates("EN-us")["messages.hello"]);
    }

    [Fact]
    public void GetAllTemplates_IsEmpty_ForUnknownLocale()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Empty(store.GetAllTemplates("fr-FR"));
    }

    // The store hands out a read-only view, not its own dictionary, so a caller cannot cast the result
    // back to Dictionary and rewrite the store's contents underneath it.
    [Fact]
    public void GetAllTemplates_DoesNotExposeTheStoreForMutation()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.IsNotType<Dictionary<string, string>>(store.GetAllTemplates("en-US"));
    }

    [Fact]
    public void GetAllTemplates_DoesNotWalkToParentCulture_ThatIsTheServicesJob()
    {
        var store = new InMemoryTranslationStore([Source("en", ("messages.hello", "Hello"))]);

        Assert.Empty(store.GetAllTemplates("en-GB"));
    }

    [Fact]
    public void Constructor_Throws_ForNullSources()
    {
        Assert.Throws<ArgumentNullException>(() => new InMemoryTranslationStore(null!));
    }

    // Golden behaviour. Locale keys are matched case-insensitively, since .NET treats culture names
    // that way everywhere else.
    [Theory]
    [InlineData("en-US")]
    [InlineData("en-us")]
    [InlineData("EN-US")]
    [InlineData("eN-uS")]
    public void GetTemplate_MatchesLocaleNames_CaseInsensitively(string locale)
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Equal("Hello", store.GetTemplate("messages.hello", locale));
    }

    [Fact]
    public void Constructor_MergesSources_DifferingOnlyByLocaleCase()
    {
        var store = new InMemoryTranslationStore(
        [
            Source("en-US", ("messages.hello", "Hello")),
            Source("en-us", ("messages.bye", "Bye")),
        ]);

        Assert.Equal("Hello", store.GetTemplate("messages.hello", "en-US"));
        Assert.Equal("Bye", store.GetTemplate("messages.bye", "en-US"));
    }

    [Theory]
    // an underscore is a different tag, not a case variant
    [InlineData("en_US")]
    // whitespace is not trimmed
    [InlineData(" en-US")]
    public void GetTemplate_ReturnsNull_ForLocaleNamesThatDifferBeyondCase(string locale)
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Null(store.GetTemplate("messages.hello", locale));
    }

    // Golden behaviour. Message ids stay case-sensitive - they are developer-chosen keys, not culture
    // names, so `messages.Hello` and `messages.hello` are deliberately distinct.
    [Fact]
    public void GetTemplate_IsCaseSensitive_ForMessageIds()
    {
        var store = new InMemoryTranslationStore([Source("en-US", ("messages.hello", "Hello"))]);

        Assert.Null(store.GetTemplate("messages.Hello", "en-US"));
    }

    // Golden behaviour. The store matches a locale exactly (bar casing); widening from en-GB to en is
    // the caller's job, and DefaultIntlService is what does it. This keeps the store contract simple
    // enough that a custom store cannot get the widening subtly wrong.
    [Fact]
    public void GetTemplate_DoesNotWalkToParentCulture_ThatIsTheServicesJob()
    {
        var store = new InMemoryTranslationStore([Source("en", ("messages.hello", "Hello"))]);

        Assert.Null(store.GetTemplate("messages.hello", "en-GB"));
    }
}
