using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Administrator use case "Configure rounds" (prize and time per round).
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("admin/rounds")]
public class AdminRoundsController : AppController
{
    private readonly RoundSettingsService roundSettingsService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="roundSettingsService">RoundSettingsService: round settings use cases.</param>
    public AdminRoundsController(RoundSettingsService roundSettingsService)
    {
        this.roundSettingsService = roundSettingsService;
    }

    /// <summary>
    /// Shows the five rounds in a single form.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the form view.</returns>
    [HttpGet("")]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var storedSettings = await roundSettingsService.GetAsync(cancellationToken);
        return View(RoundSettingsFormViewModel.FromSettings(storedSettings));
    }

    /// <summary>
    /// Validates and saves the five rounds together.
    /// </summary>
    /// <param name="roundSettingsForm">RoundSettingsFormViewModel: posted form.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the form, or the form with errors.</returns>
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(RoundSettingsFormViewModel roundSettingsForm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(nameof(Edit), roundSettingsForm);

        try
        {
            await roundSettingsService.SaveAsync(roundSettingsForm.ToSettings(), cancellationToken);
        }
        catch (DomainException ruleViolation)
        {
            AddRuleViolationToForm(ruleViolation);
            return View(nameof(Edit), roundSettingsForm);
        }

        ShowSuccess("Configuración de rondas guardada.");
        return RedirectToAction(nameof(Edit));
    }
}
