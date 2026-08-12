using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ApricotFramework.Intl.Examples.Web.Controllers;

/// <summary>
/// Returns a localized greeting, to demonstrate locale resolution end to end.
/// </summary>
[ApiController]
[Route("/api/greeting")]
public class GreetingController : ControllerBase
{
    /// <summary>
    /// The intl service.
    /// </summary>
    private readonly IIntlService intl;

    /// <summary>
    /// The framework localizer, backed by intl rather than by <c>.resx</c>.
    /// </summary>
    private readonly IStringLocalizer<GreetingController> localizer;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    /// <param name="intl">The intl service.</param>
    /// <param name="localizer">The framework localizer.</param>
    public GreetingController(IIntlService intl, IStringLocalizer<GreetingController> localizer)
    {
        this.intl = intl;
        this.localizer = localizer;
    }

    /// <summary>
    /// Gets a greeting in the locale resolved for this request.
    /// </summary>
    /// <returns>The resolved locale and the localized greeting.</returns>
    [HttpGet]
    public IActionResult GetGreeting()
    {
        return this.Ok(new
        {
            Locale = this.intl.GetCurrentLocale(),

            // named placeholders, through IIntlService
            Hello = this.intl.Format("messages.hello"),
            Welcome = this.intl.Format("messages.welcome", new Dictionary<string, object?> { ["name"] = "Apricot" }),

            // positional placeholders, through the framework's IStringLocalizer
            Required = this.localizer["validation.required", "Email"].Value,
            Missing = this.localizer["messages.absent"].ResourceNotFound,
        });
    }
}
