using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Intl.AspNetCore.Tests;

public class DefaultLocaleAccessorTests
{
    private sealed class StubOptionsMonitor(IntlSettings value) : IOptionsMonitor<IntlSettings>
    {
        public IntlSettings CurrentValue => value;

        public IntlSettings Get(string? name) => value;

        public IDisposable OnChange(Action<IntlSettings, string?> listener) => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static DefaultLocaleAccessor Accessor(HttpContext? context, string? fallbackLocale = null)
    {
        var settings = new StubOptionsMonitor(new IntlSettings { FallbackLocale = fallbackLocale });

        return new DefaultLocaleAccessor(settings, new HttpContextAccessor { HttpContext = context });
    }

    private static DefaultHttpContext ContextWithCulture(string cultureName)
    {
        var context = new DefaultHttpContext();
        var culture = new CultureInfo(cultureName);

        context.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(new RequestCulture(culture, culture), provider: null));

        return context;
    }

    [Fact]
    public void GetLocale_ReturnsNull_WhenNoHttpContext()
    {
        Assert.Null(Accessor(null).GetLocale());
    }

    [Fact]
    public void GetLocale_ReturnsRequestCultureName_WhenCultureFeaturePresent()
    {
        Assert.Equal("hy-AM", Accessor(ContextWithCulture("hy-AM")).GetLocale());
    }

    [Fact]
    public void GetLocale_PrefersRequestCulture_OverQueryString()
    {
        var context = ContextWithCulture("hy-AM");
        context.Request.QueryString = new QueryString("?locale=en-US");

        Assert.Equal("hy-AM", Accessor(context).GetLocale());
    }

    [Fact]
    public void GetLocale_ReturnsQueryStringValue_WhenNoCultureFeature()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?locale=hy-AM");

        Assert.Equal("hy-AM", Accessor(context).GetLocale());
    }

    // Regression. The invariant culture has an empty - not null - name, so a naive null check makes
    // the query-string branch unreachable whenever the localization middleware is registered.
    [Fact]
    public void GetLocale_FallsBackToQueryString_ForInvariantCultureFeature()
    {
        var context = ContextWithCulture(string.Empty);
        context.Request.QueryString = new QueryString("?locale=hy-AM");

        Assert.Equal("hy-AM", Accessor(context).GetLocale());
    }

    [Fact]
    public void GetLocale_ReturnsNull_ForInvariantCultureAndNoQueryString()
    {
        Assert.Null(Accessor(ContextWithCulture(string.Empty)).GetLocale());
    }

    [Fact]
    public void GetLocale_ReturnsNull_WhenNeitherCultureNorQueryStringPresent()
    {
        Assert.Null(Accessor(new DefaultHttpContext()).GetLocale());
    }

    [Fact]
    public void GetLocale_IgnoresUnrelatedQueryParameters()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?lang=hy-AM");

        Assert.Null(Accessor(context).GetLocale());
    }

    [Fact]
    public void GetFallbackLocale_ReturnsConfiguredLocale()
    {
        Assert.Equal("hy-AM", Accessor(null, "hy-AM").GetFallbackLocale());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFallbackLocale_ReturnsDefault_WhenNotConfigured(string? configured)
    {
        Assert.Equal("en-US", Accessor(null, configured).GetFallbackLocale());
    }

    [Fact]
    public void Constructor_Throws_ForNullSettings()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultLocaleAccessor(null!, new HttpContextAccessor()));
    }

    [Fact]
    public void Constructor_Throws_ForNullHttpContextAccessor()
    {
        var settings = new StubOptionsMonitor(new IntlSettings());

        Assert.Throws<ArgumentNullException>(() => new DefaultLocaleAccessor(settings, null!));
    }
}
