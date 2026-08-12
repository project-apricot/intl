using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Intl.AspNetCore.Tests;

public class IntlServiceCollectionExtensionsTests
{
    private sealed class CustomLocaleAccessor : ILocaleAccessor
    {
        public string? GetLocale() => "custom";

        public string GetFallbackLocale() => "custom-fallback";
    }

    private sealed class CustomTranslationStore : ITranslationStore
    {
        public string? GetTemplate(string id, string locale) => "custom-template";

        public IReadOnlyDictionary<string, string> GetAllTemplates(string locale) => new Dictionary<string, string>();
    }

    private sealed class CustomIntlService : IIntlService
    {
        public string Format(string id, string? locale, IDictionary<string, object?>? args = null, string? fallback = null) => "custom";

        public string Format(string id, IDictionary<string, object?>? args = null, string? fallback = null) => "custom";

        public string? FormatOrNull(string id, string? locale, IDictionary<string, object?>? args = null) => "custom";

        public string GetCurrentLocale() => "custom";

        public IReadOnlyDictionary<string, string> GetAllTemplates(string? locale, bool includeFallbacks = true) => new Dictionary<string, string>();
    }

    private sealed class CustomTranslationSource : ITranslationSource
    {
        public string GetLocale() => "en-US";

        public IReadOnlyDictionary<string, string> GetMessages() => new Dictionary<string, string> { ["messages.hello"] = "Hello" };
    }

    /// <summary>
    /// Counts how many times it is invoked, and whether every stream it handed out was disposed.
    /// </summary>
    private sealed class CountingStreamSupplier(string json)
    {
        private readonly List<MemoryStream> issued = [];

        public int InvocationCount { get; private set; }

        public bool AllStreamsDisposed => this.issued.TrueForAll(s => !s.CanRead);

        public MemoryStream Supply()
        {
            this.InvocationCount++;

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            this.issued.Add(stream);

            return stream;
        }
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
    }

    [Fact]
    public void AddIntlCore_RegistersTheDefaultServices()
    {
        var provider = new ServiceCollection().AddIntlCore(Configuration()).BuildServiceProvider();

        Assert.IsType<DefaultLocaleAccessor>(provider.GetRequiredService<ILocaleAccessor>());
        Assert.IsType<InMemoryTranslationStore>(provider.GetRequiredService<ITranslationStore>());
        Assert.IsType<DefaultIntlService>(provider.GetRequiredService<IIntlService>());
    }

    [Fact]
    public void AddIntlCore_BindsSettings_FromTheIntlSection()
    {
        var provider = new ServiceCollection()
            .AddIntlCore(Configuration(("Intl:FallbackLocale", "hy-AM")))
            .BuildServiceProvider();

        Assert.Equal("hy-AM", provider.GetRequiredService<IOptions<IntlSettings>>().Value.FallbackLocale);
    }

    [Fact]
    public void AddIntlCore_ResolvesIntlService_WithNoTranslationSources()
    {
        var provider = new ServiceCollection().AddIntlCore(Configuration()).BuildServiceProvider();

        // no sources registered, so every id falls through to itself
        Assert.Equal("messages.hello", provider.GetRequiredService<IIntlService>().Format("messages.hello"));
    }

    [Fact]
    public void AddIntlCore_IsIdempotent()
    {
        var provider = new ServiceCollection()
            .AddIntlCore(Configuration())
            .AddIntlCore(Configuration())
            .BuildServiceProvider();

        Assert.IsType<DefaultIntlService>(provider.GetRequiredService<IIntlService>());
    }

    // AddIntlCore uses TryAdd, so it never replaces a registration already present. Note this is not
    // an ordering requirement - see AddIntlService_Overrides_WhenRegisteredAfterAddIntlCore, which
    // pins that registering afterwards wins too, because the container resolves the last descriptor.
    [Fact]
    public void AddIntlCore_DoesNotOverride_ServicesRegisteredBeforeIt()
    {
        var provider = new ServiceCollection()
            .AddLocaleAccessor<CustomLocaleAccessor>()
            .AddTranslationStore<CustomTranslationStore>()
            .AddIntlService<CustomIntlService>()
            .AddIntlCore(Configuration())
            .BuildServiceProvider();

        Assert.IsType<CustomLocaleAccessor>(provider.GetRequiredService<ILocaleAccessor>());
        Assert.IsType<CustomTranslationStore>(provider.GetRequiredService<ITranslationStore>());
        Assert.IsType<CustomIntlService>(provider.GetRequiredService<IIntlService>());
    }

    [Fact]
    public void AddLocaleAccessor_Instance_IsResolved()
    {
        var accessor = new CustomLocaleAccessor();

        var provider = new ServiceCollection().AddLocaleAccessor(accessor).BuildServiceProvider();

        Assert.Same(accessor, provider.GetRequiredService<ILocaleAccessor>());
    }

    [Fact]
    public void AddTranslationStore_Instance_IsResolved()
    {
        var store = new CustomTranslationStore();

        var provider = new ServiceCollection().AddTranslationStore(store).BuildServiceProvider();

        Assert.Same(store, provider.GetRequiredService<ITranslationStore>());
    }

    [Fact]
    public void AddIntlService_Instance_IsResolved()
    {
        var service = new CustomIntlService();

        var provider = new ServiceCollection().AddIntlService(service).BuildServiceProvider();

        Assert.Same(service, provider.GetRequiredService<IIntlService>());
    }

    [Fact]
    public void AddTranslationSource_Instance_IsResolved()
    {
        var source = new CustomTranslationSource();

        var provider = new ServiceCollection().AddTranslationSource(source).BuildServiceProvider();

        Assert.Same(source, provider.GetRequiredService<ITranslationSource>());
    }

    [Fact]
    public void AddTranslationSource_Generic_IsResolved()
    {
        var provider = new ServiceCollection().AddTranslationSource<CustomTranslationSource>().BuildServiceProvider();

        Assert.IsType<CustomTranslationSource>(provider.GetRequiredService<ITranslationSource>());
    }

    [Fact]
    public void AddTranslationSource_FeedsTheStore_ThroughAddIntlCore()
    {
        var provider = new ServiceCollection()
            .AddTranslationSource<CustomTranslationSource>()
            .AddIntlCore(Configuration(("Intl:FallbackLocale", "en-US")))
            .BuildServiceProvider();

        Assert.Equal("Hello", provider.GetRequiredService<IIntlService>().Format("messages.hello"));
    }

    [Fact]
    public void AddJsonTranslationSource_RegistersTheParsedMessages()
    {
        var supplier = new CountingStreamSupplier("""{"messages.hello":"Hello"}""");

        var provider = new ServiceCollection()
            .AddJsonTranslationSource("en-US", supplier.Supply)
            .BuildServiceProvider();

        var source = provider.GetRequiredService<ITranslationSource>();

        Assert.Equal("en-US", source.GetLocale());
        Assert.Equal("Hello", source.GetMessages()["messages.hello"]);
    }

    // Regression. The supplier used to be invoked twice: once into a `using` that was never read, then
    // again for the deserialization, and that second stream was never disposed - the stream overload of
    // JsonSerializer.Deserialize does not dispose what it is handed. Registration runs once per source
    // at startup, so the effect was bounded by the number of locales, and FileStream's finalizer
    // reclaimed the handles eventually: non-deterministic release and twice the I/O, not an unbounded
    // leak. Still worth pinning, since a refactor could silently reintroduce it and no functional test
    // would notice.
    [Fact]
    public void AddJsonTranslationSource_InvokesSupplierOnce_AndDisposesTheStream()
    {
        var supplier = new CountingStreamSupplier("""{"messages.hello":"Hello"}""");

        new ServiceCollection().AddJsonTranslationSource("en-US", supplier.Supply);

        Assert.Equal(1, supplier.InvocationCount);
        Assert.True(supplier.AllStreamsDisposed, "every stream handed out by the supplier should be disposed");
    }

    [Fact]
    public void AddJsonTranslationSource_ReadsEagerly_SoMalformedJsonThrowsAtRegistration()
    {
        var supplier = new CountingStreamSupplier("{ not json");

        Assert.Throws<JsonException>(() => new ServiceCollection().AddJsonTranslationSource("en-US", supplier.Supply));
    }

    [Fact]
    public void AddJsonTranslationSource_Throws_ForNullJsonDocument()
    {
        var supplier = new CountingStreamSupplier("null");

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddJsonTranslationSource("en-US", supplier.Supply));
    }

    [Fact]
    public void AddJsonTranslationSource_SupportsMultipleLocales()
    {
        var english = new CountingStreamSupplier("""{"messages.hello":"Hello"}""");
        var armenian = new CountingStreamSupplier("""{"messages.hello":"Բարև"}""");

        var provider = new ServiceCollection()
            .AddJsonTranslationSource("en-US", english.Supply)
            .AddJsonTranslationSource("hy-AM", armenian.Supply)
            .AddIntlCore(Configuration(("Intl:FallbackLocale", "en-US")))
            .BuildServiceProvider();

        var intl = provider.GetRequiredService<IIntlService>();

        Assert.Equal("Hello", intl.Format("messages.hello", "en-US"));
        Assert.Equal("Բարև", intl.Format("messages.hello", "hy-AM"));
    }

    [Fact]
    public void AddTranslationSource_IsFound_WhenRegisteredAfterAddIntlCore()
    {
        var provider = new ServiceCollection()
            .AddIntlCore(Configuration(("Intl:FallbackLocale", "en-US")))
            .AddTranslationSource<CustomTranslationSource>()
            .BuildServiceProvider();

        Assert.Equal("Hello", provider.GetRequiredService<IIntlService>().Format("messages.hello"));
    }

    [Fact]
    public void AddIntlService_Overrides_WhenRegisteredAfterAddIntlCore()
    {
        var provider = new ServiceCollection()
            .AddIntlCore(Configuration())
            .AddIntlService<CustomIntlService>()
            .BuildServiceProvider();

        Assert.IsType<CustomIntlService>(provider.GetRequiredService<IIntlService>());
    }

    [Fact]
    public void AddIntlCore_Throws_ForNullServices()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddIntlCore(Configuration()));
    }

    [Fact]
    public void AddIntlCore_Throws_ForNullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddIntlCore(null!));
    }

    [Fact]
    public void AddJsonTranslationSource_Throws_ForNullSupplier()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddJsonTranslationSource("en-US", null!));
    }

    [Fact]
    public void AddJsonTranslationSource_Throws_ForNullLocale()
    {
        var supplier = new CountingStreamSupplier("{}");

        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddJsonTranslationSource(null!, supplier.Supply));
    }
}
