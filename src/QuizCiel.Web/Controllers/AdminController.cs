using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Administration home: shows whether the game is ready to be played.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("admin")]
public class AdminController : AppController
{
    private readonly GameReadinessService readinessService;
    private readonly CategoryService categoryService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="readinessService">GameReadinessService: readiness checks.</param>
    /// <param name="categoryService">CategoryService: category overview.</param>
    public AdminController(GameReadinessService readinessService, CategoryService categoryService)
    {
        this.readinessService = readinessService;
        this.categoryService = categoryService;
    }

    /// <summary>
    /// Shows the readiness report and the categories.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the dashboard view.</returns>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var readinessReport = await readinessService.GetReportAsync(cancellationToken);
        var categorySummaries = await categoryService.GetSummariesAsync(cancellationToken);
        return View(new AdminDashboardViewModel(readinessReport, categorySummaries));
    }
}
