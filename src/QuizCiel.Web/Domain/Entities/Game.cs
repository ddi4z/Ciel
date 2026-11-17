using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Domain.States;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// Aggregate root of a single player's game. All rule decisions are delegated to its current
/// <see cref="IGameState"/> (State pattern), so a finished game rejects every action.
/// </summary>
public class Game
{
    internal const string AlreadyFinishedMessage = "La partida ya terminó.";

    public Guid Id { get; private set; }
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;
    public GameStatus Status { get; private set; }
    public int CurrentRoundNumber { get; private set; }
    public decimal AccumulatedPrize { get; private set; }
    public decimal? FinalPrize { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }
    public List<GameRound> Rounds { get; private set; } = new();

    public bool IsFinished => Status != GameStatus.InProgress;
    public GameRound CurrentRound => Rounds.Single(round => round.RoundNumber == CurrentRoundNumber);
    public bool CurrentRoundHasQuestion => Rounds.Any(round => round.RoundNumber == CurrentRoundNumber);
    private IGameState State => IsFinished ? FinishedState.Instance : InProgressState.Instance;

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Start"/>.
    /// </summary>
    private Game()
    {
    }

    /// <summary>
    /// Factory method that starts a game in round 1 with nothing accumulated.
    /// </summary>
    /// <param name="player">Player: who plays the game.</param>
    /// <param name="firstRoundConfig">RoundConfig: settings of round 1.</param>
    /// <param name="firstQuestion">Question: active level 1 question, with category and options loaded.</param>
    /// <param name="startedAtUtc">DateTime: start instant in UTC.</param>
    /// <returns>Game: the new, not yet persisted game.</returns>
    public static Game Start(Player player, RoundConfig firstRoundConfig, Question firstQuestion, DateTime startedAtUtc)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Player = player,
            Status = GameStatus.InProgress,
            CurrentRoundNumber = 1,
            StartedAt = startedAtUtc,
            ConcurrencyStamp = Guid.NewGuid()
        };
        game.AssignQuestion(firstRoundConfig, firstQuestion, startedAtUtc);
        return game;
    }

    /// <summary>
    /// Answers the current question; delegated to the current state.
    /// </summary>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>AnswerOutcome: what happened as a result of the answer.</returns>
    public AnswerOutcome Answer(int optionId, DateTime nowUtc) => State.Answer(this, optionId, nowUtc);

    /// <summary>
    /// Voluntarily leaves the game keeping the accumulated prize; delegated to the current state.
    /// </summary>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>GameStatus: final status (Withdrawn, or ForcedEnd if the time had already run out).</returns>
    public GameStatus Withdraw(DateTime nowUtc) => State.Withdraw(this, nowUtc);

    /// <summary>
    /// Applies the forced end when the time to answer is over; delegated to the current state.
    /// </summary>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>bool: true when the game was just ended because of the timeout.</returns>
    public bool ExpireIfTimedOut(DateTime nowUtc) => State.ExpireIfTimedOut(this, nowUtc);

    /// <summary>
    /// Asks the question of the current round. Business rules: only while in progress, one question per
    /// round, and the question must come from the category whose level equals the round number.
    /// </summary>
    /// <param name="roundConfig">RoundConfig: settings of the current round.</param>
    /// <param name="question">Question: active question with category and options loaded.</param>
    /// <param name="askedAtUtc">DateTime: instant the question is shown, in UTC.</param>
    public void AssignQuestion(RoundConfig roundConfig, Question question, DateTime askedAtUtc)
    {
        if (IsFinished)
            throw new DomainException(AlreadyFinishedMessage);
        if (CurrentRoundHasQuestion)
            throw new DomainException("La ronda actual ya tiene una pregunta asignada.");
        if (roundConfig.RoundNumber != CurrentRoundNumber)
            throw new DomainException("La configuración no corresponde a la ronda actual.");
        if (!question.IsActive || question.Category.Level != CurrentRoundNumber)
            throw new DomainException("La pregunta no corresponde al nivel de la ronda actual.");

        Rounds.Add(GameRound.Create(roundConfig, question, askedAtUtc));
    }

    /// <summary>
    /// Tells whether the game was played by the given login account. Requires the player to be loaded.
    /// </summary>
    /// <param name="userId">string: identifier of the login account.</param>
    /// <returns>bool: true when the account owns the game.</returns>
    public bool BelongsTo(string userId)
    {
        return Player.UserId == userId;
    }

    /// <summary>
    /// Assigns a new concurrency token so a concurrent update of the same game is detected on save.
    /// </summary>
    public void RenewConcurrencyStamp()
    {
        ConcurrencyStamp = Guid.NewGuid();
    }

    /// <summary>
    /// Adds the prize of a correctly answered round. Only game states may call it.
    /// </summary>
    /// <param name="roundPrize">decimal: prize of the round just won.</param>
    internal void AddPrize(decimal roundPrize)
    {
        AccumulatedPrize += roundPrize;
    }

    /// <summary>
    /// Moves the game to the next round. Only game states may call it.
    /// </summary>
    internal void LevelUp()
    {
        CurrentRoundNumber++;
    }

    /// <summary>
    /// Closes the game with a final status and prize. Only game states may call it.
    /// </summary>
    /// <param name="finalStatus">GameStatus: any status except InProgress.</param>
    /// <param name="finalPrize">decimal: prize the player takes home.</param>
    /// <param name="endedAtUtc">DateTime: end instant in UTC.</param>
    internal void Finish(GameStatus finalStatus, decimal finalPrize, DateTime endedAtUtc)
    {
        Status = finalStatus;
        FinalPrize = finalPrize;
        EndedAt = endedAtUtc;
    }
}
