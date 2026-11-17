using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Administrator use cases "View games" and "View prizes to pay".
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("admin")]
public class AdminGamesController : AppController
{
    private readonly GameService gameService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="gameService">GameService: game use cases.</param>
    public AdminGamesController(GameService gameService)
    {
        this.gameService = gameService;
    }

    /// <summary>
    /// Lists the most recent games, optionally filtered by player name or cédula.
    /// </summary>
    /// <param name="playerFilter">string?: text contained in the name or cédula.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the games view.</returns>
    [HttpGet("games")]
    public async Task<IActionResult> Games(string? playerFilter, CancellationToken cancellationToken)
    {
        var gamesViewModel = new AdminGamesViewModel
        {
            PlayerFilter = playerFilter,
            Games = await gameService.GetHistoryAsync(playerFilter, cancellationToken)
        };
        return View(gamesViewModel);
    }

    /// <summary>
    /// Lists who must be paid and how much (finished games with a prize greater than zero).
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the prizes view.</returns>
    [HttpGet("prizes")]
    public async Task<IActionResult> Prizes(CancellationToken cancellationToken)
    {
        var gamesWithPrize = await gameService.GetPrizesToPayAsync(cancellationToken);
        return View(new PrizesToPayViewModel(gamesWithPrize));
    }
}
