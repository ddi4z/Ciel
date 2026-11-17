using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Abstractions;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Player use cases: start, play, answer and withdraw. Every POST redirects (Post/Redirect/Get).
/// </summary>
[Authorize]
[Route("game")]
public class GameController : AppController
{
    private readonly GameService gameService;
    private readonly IClock clock;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="gameService">GameService: game use cases.</param>
    /// <param name="clock">IClock: current time, used for the countdown.</param>
    public GameController(GameService gameService, IClock clock)
    {
        this.gameService = gameService;
        this.clock = clock;
    }

    /// <summary>
    /// Starts the only game of the signed-in player.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the game, or back home with an error.</returns>
    [HttpPost("start")]
    [Authorize(Roles = AppRoles.Player)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        try
        {
            var gameId = await gameService.StartAsync(CurrentUserId, cancellationToken);
            return RedirectToAction(nameof(Play), new { gameId });
        }
        catch (DomainException ruleViolation)
        {
            ShowRuleViolation(ruleViolation);
            return Redirect("/");
        }
    }

    /// <summary>
    /// Shows the current question, or the result when the game is over. Visible to its player and to administrators.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: play view or result view.</returns>
    [HttpGet("{gameId:guid}")]
    public async Task<IActionResult> Play(Guid gameId, CancellationToken cancellationToken)
    {
        var viewerIsAdmin = User.IsInRole(AppRoles.Admin);
        var game = await gameService.GetForViewerAsync(gameId, CurrentUserId, viewerIsAdmin, cancellationToken);
        if (game.IsFinished || viewerIsAdmin)
            return View("Result", GameResultViewModel.From(game));

        var ladderRounds = await gameService.GetPrizeLadderAsync(cancellationToken);
        return View(GamePlayViewModel.From(game, ladderRounds, clock.GetUtcNow()));
    }

    /// <summary>
    /// Answers the current question.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect back to the game.</returns>
    [HttpPost("{gameId:guid}/answer")]
    [Authorize(Roles = AppRoles.Player)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer(Guid gameId, int optionId, CancellationToken cancellationToken)
    {
        try
        {
            var answerOutcome = await gameService.AnswerAsync(gameId, CurrentUserId, optionId, cancellationToken);
            if (answerOutcome == AnswerOutcome.CorrectAndLeveledUp)
                ShowSuccess("¡Respuesta correcta! Subes de nivel.");
        }
        catch (DomainException ruleViolation)
        {
            ShowRuleViolation(ruleViolation);
        }
        return RedirectToAction(nameof(Play), new { gameId });
    }

    /// <summary>
    /// Withdraws from the game keeping the accumulated prize.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the game result.</returns>
    [HttpPost("{gameId:guid}/withdraw")]
    [Authorize(Roles = AppRoles.Player)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(Guid gameId, CancellationToken cancellationToken)
    {
        try
        {
            await gameService.WithdrawAsync(gameId, CurrentUserId, cancellationToken);
        }
        catch (DomainException ruleViolation)
        {
            ShowRuleViolation(ruleViolation);
        }
        return RedirectToAction(nameof(Play), new { gameId });
    }
}
