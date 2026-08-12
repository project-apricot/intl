using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// Hands out <see cref="IntlStringLocalizer"/> instances. This is the registration that redirects the
/// framework's localization away from <c>.resx</c>: <c>IViewLocalizer</c>, <c>IHtmlLocalizer</c> and
/// DataAnnotations all reach their localizer through an <see cref="IStringLocalizerFactory"/>.
/// </summary>
/// <remarks>
/// Both <c>Create</c> overloads ignore what they are given. A <c>.resx</c> factory uses the type or
/// base name to choose a resource file; message ids here are one flat namespace, so there is nothing
/// to choose between — every caller gets a localizer over the same messages.
/// </remarks>
public class IntlStringLocalizerFactory : IStringLocalizerFactory
{
    /// <summary>
    /// The single localizer handed to every caller. It holds no per-request state — the locale is
    /// resolved on each lookup — so one instance serves all of them.
    /// </summary>
    private readonly IntlStringLocalizer localizer;

    /// <summary>
    /// Creates a new instance of the factory.
    /// </summary>
    /// <param name="intl">The intl service.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="intl"/> is null.</exception>
    public IntlStringLocalizerFactory(IIntlService intl)
    {
        ArgumentNullException.ThrowIfNull(intl);

        this.localizer = new IntlStringLocalizer(intl);
    }

    /// <summary>
    /// Creates a localizer for a resource source.
    /// </summary>
    /// <param name="resourceSource">The type the resources nominally belong to. Ignored.</param>
    /// <returns>A localizer over every message.</returns>
    public virtual IStringLocalizer Create(Type resourceSource)
    {
        return this.localizer;
    }

    /// <summary>
    /// Creates a localizer for a resource base name and location.
    /// </summary>
    /// <param name="baseName">The resource base name. Ignored.</param>
    /// <param name="location">The resource location. Ignored.</param>
    /// <returns>A localizer over every message.</returns>
    public virtual IStringLocalizer Create(string baseName, string location)
    {
        return this.localizer;
    }
}
