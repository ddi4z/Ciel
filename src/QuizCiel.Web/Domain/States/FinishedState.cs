using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.States;

/// <summary>
/// Rules of a game that already ended (won, withdrawn, lost or forced end): no action is allowed.
/// </summary>
public sealed class FinishedState : IGameState
{
    public static readonly FinishedState Instance = new();

    /// <summary>
    /// Private because the state is stateless and shared through <see cref="Instance"/>.
    /// </summary>
    private FinishedState()
    {
    }

    /// <summary>
    /// Always rejected: a finished game cannot be answered.
    /// </summary>
    /// <param name="game">Game: finished game.</param>
    /// <param name="optionId">int: ignored.</param>
    /// <param name="nowUtc">DateTime: ignored.</param>
    /// <returns>AnswerOutcome: never returns.</returns>
    public AnswerOutcome Answer(Game game, int optionId, DateTime nowUtc)
    {
        throw new DomainException(Game.AlreadyFinishedMessage);
    }

    /// <summary>
    /// Always rejected: a finished game cannot be abandoned.
    /// </summary>
    /// <param name="game">Game: finished game.</param>
    /// <param name="nowUtc">DateTime: ignored.</param>
    /// <returns>GameStatus: never returns.</returns>
    public GameStatus Withdraw(Game game, DateTime nowUtc)
    {
        throw new DomainException(Game.AlreadyFinishedMessage);
    }

    /// <summary>
    /// A finished game never expires again.
    /// </summary>
    /// <param name="game">Game: finished game.</param>
    /// <param name="nowUtc">DateTime: ignored.</param>
    /// <returns>bool: always false.</returns>
    public bool ExpireIfTimedOut(Game game, DateTime nowUtc)
    {
        return false;
    }
}
