using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.AspNetCore;

/// <summary>
/// The <see cref="IStringLocalizer{T}"/> form of <see cref="IntlStringLocalizer"/>, so a type can ask
/// for <c>IStringLocalizer&lt;MyController&gt;</c> the way the framework expects.
/// </summary>
/// <typeparam name="T">
/// The type the resources nominally belong to. <b>Ignored.</b> A <c>.resx</c>-backed localizer uses it
/// to pick a resource file; message ids here are one flat namespace, so
/// <c>IStringLocalizer&lt;A&gt;["x"]</c> and <c>IStringLocalizer&lt;B&gt;["x"]</c> resolve to the same
/// message. Scope ids yourself if you need separation — <c>"account.signin.title"</c> rather than
/// <c>"title"</c>.
/// </typeparam>
public class IntlStringLocalizer<T> : IntlStringLocalizer, IStringLocalizer<T>
{
    /// <summary>
    /// Creates a new instance of the localizer.
    /// </summary>
    /// <param name="intl">The intl service.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="intl"/> is null.</exception>
    public IntlStringLocalizer(IIntlService intl)
        : base(intl)
    {
    }
}
