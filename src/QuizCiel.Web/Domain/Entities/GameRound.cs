using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// One round played inside a game: the question asked, the answer given and the prize at stake.
/// Prize and time limit are copied from the configuration when the question is asked, so later
/// configuration changes never alter a game that is already running.
/// </summary>
public class GameRound
{
    public int Id { get; private set; }
    public Guid GameId { get; private set; }
    public int RoundNumber { get; private set; }
    public int QuestionId { get; private set; }
    public Question Question { get; private set; } = null!;
    public int? SelectedOptionId { get; private set; }
    public bool? IsCorrect { get; private set; }
    public decimal Prize { get; private set; }
    public int TimeLimitSeconds { get; private set; }
    public DateTime AskedAt { get; private set; }
    public DateTime? AnsweredAt { get; private set; }

    public bool IsAnswered => SelectedOptionId.HasValue;
    public DateTime DeadlineUtc => AskedAt.AddSeconds(TimeLimitSeconds);

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private GameRound()
    {
    }

    /// <summary>
    /// Creates the round that asks the given question. Only <see cref="Game"/> may call it.
    /// </summary>
    /// <param name="roundConfig">RoundConfig: settings of the round being started.</param>
    /// <param name="question">Question: active question of the matching category level, with options loaded.</param>
    /// <param name="askedAtUtc">DateTime: instant the question is shown, in UTC.</param>
    /// <returns>GameRound: the new round.</returns>
    internal static GameRound Create(RoundConfig roundConfig, Question question, DateTime askedAtUtc)
    {
        return new GameRound
        {
            RoundNumber = roundConfig.RoundNumber,
            QuestionId = question.Id,
            Question = question,
            Prize = roundConfig.Prize,
            TimeLimitSeconds = roundConfig.TimeLimitSeconds,
            AskedAt = askedAtUtc
        };
    }

    /// <summary>
    /// Business rule: the answer is accepted until the deadline plus a small grace period for network latency.
    /// </summary>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>bool: true when the time to answer is over.</returns>
    public bool HasExpired(DateTime nowUtc)
    {
        return !IsAnswered && nowUtc > DeadlineUtc.AddSeconds(GameRules.AnswerGracePeriodSeconds);
    }

    /// <summary>
    /// Records the player's choice. Only <see cref="Game"/> may call it.
    /// </summary>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="answeredAtUtc">DateTime: instant of the answer, in UTC.</param>
    /// <returns>bool: true when the chosen option is the correct one.</returns>
    internal bool RegisterAnswer(int optionId, DateTime answeredAtUtc)
    {
        if (IsAnswered)
            throw new DomainException("Esta pregunta ya fue respondida.");

        var chosenOption = Question.GetOption(optionId);
        SelectedOptionId = chosenOption.Id;
        IsCorrect = chosenOption.IsCorrect;
        AnsweredAt = answeredAtUtc;
        return chosenOption.IsCorrect;
    }
}
