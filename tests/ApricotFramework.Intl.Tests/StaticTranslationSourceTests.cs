namespace ApricotFramework.Intl.Tests;

public class StaticTranslationSourceTests
{
    [Fact]
    public void GetLocale_ReturnsConstructorLocale()
    {
        var source = new StaticTranslationSource("en-US", new Dictionary<string, string>());

        Assert.Equal("en-US", source.GetLocale());
    }

    [Fact]
    public void GetMessages_ReturnsConstructorMessages()
    {
        var source = new StaticTranslationSource("en-US", new Dictionary<string, string>
        {
            ["messages.hello"] = "Hello",
        });

        Assert.Equal("Hello", source.GetMessages()["messages.hello"]);
    }

    [Fact]
    public void GetMessages_IsIsolated_FromLaterCallerMutation()
    {
        // the source must not be a live view over the caller's dictionary
        var supplied = new Dictionary<string, string> { ["messages.hello"] = "Hello" };
        var source = new StaticTranslationSource("en-US", supplied);

        supplied["messages.hello"] = "Mutated";
        supplied["messages.added"] = "Added";

        Assert.Equal("Hello", source.GetMessages()["messages.hello"]);
        Assert.DoesNotContain("messages.added", source.GetMessages());
    }

    [Fact]
    public void Constructor_Throws_ForNullLocale()
    {
        Assert.Throws<ArgumentNullException>(() => new StaticTranslationSource(null!, new Dictionary<string, string>()));
    }

    [Fact]
    public void Constructor_Throws_ForNullMessages()
    {
        Assert.Throws<ArgumentNullException>(() => new StaticTranslationSource("en-US", null!));
    }
}
