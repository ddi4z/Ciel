using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Home page and error pages.
/// </summary>
public class HomeController : AppController
{
    private readonly GameService gameService;
    private readonly GameReadinessService readinessService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="gameService">GameService: game use cases.</param>
    /// <param name="readinessService">GameReadinessService: checks if the game can be played.</param>
    public HomeController(GameService gameService, GameReadinessService readinessService)
    {
        this.gameService = gameService;
        this.readinessService = readinessService;
    }

    /// <summary>
    /// Shows the rules and prize ladder; a signed-in player also sees their participation and prize.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the home view.</returns>
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var readinessReport = await readinessService.GetReportAsync(cancellationToken);
        var ladderRounds = await gameService.GetPrizeLadderAsync(cancellationToken);
        var homeViewModel = new HomeViewModel
        {
            IsGameReady = readinessReport.IsReady,
            PrizeLadder = PrizeStepViewModel.BuildLadder(ladderRounds, currentRoundNumber: null)
        };

        if (User.IsInRole(AppRoles.Player))
        {
            var participation = await gameService.GetParticipationAsync(CurrentUserId, cancellationToken);
            homeViewModel.Participation = ParticipationViewModel.From(participation);
        }
        return View(homeViewModel);
    }

    /// <summary>
    /// Shows a friendly page for an HTTP error status code. It accepts any HTTP method because the status
    /// code pages middleware re-executes it with the method of the failed request (for example a POST).
    /// </summary>
    /// <param name="statusCode">int: HTTP status code.</param>
    /// <returns>IActionResult: the error view.</returns>
    [Route("/error/{statusCode:int}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;
        return View();
    }
}
