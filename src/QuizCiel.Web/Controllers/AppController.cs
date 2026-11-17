using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Base controller with the flash messages shared by every page (shown once after a redirect) and the
/// handling of business rules rejected by the domain.
/// </summary>
public abstract class AppController : Controller
{
    public const string SuccessMessageKey = "FlashSuccess";
    public const string ErrorMessageKey = "FlashError";

    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private ILogger Logger => HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    /// <summary>
    /// Logs a rejected business rule and shows it as a flash message on the page the action redirects to.
    /// </summary>
    /// <param name="ruleViolation">DomainException: rule rejected by the domain.</param>
    protected void ShowRuleViolation(DomainException ruleViolation)
    {
        LogRuleViolation(ruleViolation);
        ShowError(ruleViolation.Message);
    }

    /// <summary>
    /// Logs a rejected business rule and shows it at the top of the form being re-rendered.
    /// </summary>
    /// <param name="ruleViolation">DomainException: rule rejected by the domain.</param>
    protected void AddRuleViolationToForm(DomainException ruleViolation)
    {
        LogRuleViolation(ruleViolation);
        ModelState.AddModelError(string.Empty, ruleViolation.Message);
    }

    /// <summary>
    /// Records which account and request hit a business rule; expected situations, so they are not errors.
    /// </summary>
    /// <param name="ruleViolation">DomainException: rule rejected by the domain.</param>
    private void LogRuleViolation(DomainException ruleViolation)
    {
        Logger.LogInformation(
            "Business rule rejected {Method} {Path} for account {UserId}: {Reason}",
            Request.Method, Request.Path, string.IsNullOrEmpty(CurrentUserId) ? "anonymous" : CurrentUserId, ruleViolation.Message);
    }

    /// <summary>
    /// Stores a success message for the next rendered page.
    /// </summary>
    /// <param name="message">string: text to show.</param>
    protected void ShowSuccess(string message)
    {
        TempData[SuccessMessageKey] = message;
    }

    /// <summary>
    /// Stores an error message for the next rendered page.
    /// </summary>
    /// <param name="message">string: text to show.</param>
    protected void ShowError(string message)
    {
        TempData[ErrorMessageKey] = message;
    }
}
