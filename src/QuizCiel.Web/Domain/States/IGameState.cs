using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;

namespace QuizCiel.Web.Domain.States;

/// <summary>
/// State pattern: decides which actions a game allows and what they do, depending on its lifecycle state.
/// </summary>
public interface IGameState
{
    /// <summary>
    /// Answers the current question of the game.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>AnswerOutcome: what happened as a result of the answer.</returns>
    AnswerOutcome Answer(Game game, int optionId, DateTime nowUtc);

    /// <summary>
    /// Leaves the game voluntarily.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>GameStatus: final status of the game.</returns>
    GameStatus Withdraw(Game game, DateTime nowUtc);

    /// <summary>
    /// Ends the game when the time to answer is over.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>bool: true when the game was just ended because of the timeout.</returns>
    bool ExpireIfTimedOut(Game game, DateTime nowUtc);
}
