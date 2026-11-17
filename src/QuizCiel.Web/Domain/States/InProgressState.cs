using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.States;

/// <summary>
/// Rules of a game that is still being played.
/// </summary>
public sealed class InProgressState : IGameState
{
    public static readonly InProgressState Instance = new();

    /// <summary>
    /// Private because the state is stateless and shared through <see cref="Instance"/>.
    /// </summary>
    private InProgressState()
    {
    }

    /// <summary>
    /// Business rules: a late answer is a forced end without prize; a wrong answer loses everything;
    /// a correct answer adds the round prize and levels up, or wins the game in the last round.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>AnswerOutcome: what happened as a result of the answer.</returns>
    public AnswerOutcome Answer(Game game, int optionId, DateTime nowUtc)
    {
        if (ExpireIfTimedOut(game, nowUtc))
            return AnswerOutcome.TimedOut;

        var currentRound = game.CurrentRound;
        var isCorrect = currentRound.RegisterAnswer(optionId, nowUtc);
        if (!isCorrect)
        {
            game.Finish(GameStatus.Lost, finalPrize: 0, nowUtc);
            return AnswerOutcome.Wrong;
        }

        game.AddPrize(currentRound.Prize);
        if (currentRound.RoundNumber == GameRules.TotalRounds)
        {
            game.Finish(GameStatus.Won, game.AccumulatedPrize, nowUtc);
            return AnswerOutcome.CorrectAndWonGame;
        }

        game.LevelUp();
        return AnswerOutcome.CorrectAndLeveledUp;
    }

    /// <summary>
    /// Business rules: the player may leave only before answering the current question and keeps the
    /// accumulated prize; if the time already ran out the game is force-ended instead.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>GameStatus: Withdrawn, or ForcedEnd when the time was over.</returns>
    public GameStatus Withdraw(Game game, DateTime nowUtc)
    {
        if (ExpireIfTimedOut(game, nowUtc))
            return GameStatus.ForcedEnd;
        if (game.CurrentRound.IsAnswered)
            throw new DomainException("No puedes retirarte después de responder la pregunta.");

        game.Finish(GameStatus.Withdrawn, game.AccumulatedPrize, nowUtc);
        return GameStatus.Withdrawn;
    }

    /// <summary>
    /// Business rule: when the time to answer is over, the game ends without any prize.
    /// </summary>
    /// <param name="game">Game: game being played.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>bool: true when the game was just ended because of the timeout.</returns>
    public bool ExpireIfTimedOut(Game game, DateTime nowUtc)
    {
        if (!game.CurrentRound.HasExpired(nowUtc))
            return false;

        game.Finish(GameStatus.ForcedEnd, finalPrize: 0, nowUtc);
        return true;
    }
}
