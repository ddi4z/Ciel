using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// The complete set of round settings, validated as a whole because its rules span several rounds
/// (for example, every round must pay more than the previous one).
/// </summary>
public sealed class GameConfiguration
{
    public IReadOnlyList<RoundSettings> Rounds { get; }

    /// <summary>
    /// Stores rounds that were already validated; code must use <see cref="Create"/>.
    /// </summary>
    /// <param name="rounds">IReadOnlyList&lt;RoundSettings&gt;: validated rounds ordered by number.</param>
    private GameConfiguration(IReadOnlyList<RoundSettings> rounds)
    {
        Rounds = rounds;
    }

    /// <summary>
    /// Factory method that only returns a configuration satisfying every round rule.
    /// </summary>
    /// <param name="roundSettings">IEnumerable&lt;RoundSettings&gt;: the settings of all rounds, in any order.</param>
    /// <returns>GameConfiguration: the validated configuration ordered by round number.</returns>
    public static GameConfiguration Create(IEnumerable<RoundSettings> roundSettings)
    {
        var orderedRounds = roundSettings.OrderBy(round => round.RoundNumber).ToList();

        EnsureAllRoundsArePresent(orderedRounds);
        foreach (var round in orderedRounds)
        {
            EnsurePrizeIsValid(round);
            EnsureTimeLimitIsValid(round);
        }
        EnsurePrizesIncrease(orderedRounds);

        return new GameConfiguration(orderedRounds);
    }

    /// <summary>
    /// Business rule: rounds 1 to 5 must all be configured, each exactly once.
    /// </summary>
    /// <param name="orderedRounds">IReadOnlyList&lt;RoundSettings&gt;: settings ordered by round number.</param>
    private static void EnsureAllRoundsArePresent(IReadOnlyList<RoundSettings> orderedRounds)
    {
        var expectedRoundNumbers = Enumerable.Range(1, GameRules.TotalRounds);
        if (!orderedRounds.Select(round => round.RoundNumber).SequenceEqual(expectedRoundNumbers))
            throw new DomainException($"Deben configurarse exactamente las rondas 1 a {GameRules.TotalRounds}.");
    }

    /// <summary>
    /// Business rule: prizes are positive whole pesos.
    /// </summary>
    /// <param name="round">RoundSettings: round to validate.</param>
    private static void EnsurePrizeIsValid(RoundSettings round)
    {
        if (round.Prize <= 0 || decimal.Truncate(round.Prize) != round.Prize)
            throw new DomainException($"El premio de la ronda {round.RoundNumber} debe ser un valor entero mayor que cero.");
    }

    /// <summary>
    /// Business rule: the answer time stays within the allowed range.
    /// </summary>
    /// <param name="round">RoundSettings: round to validate.</param>
    private static void EnsureTimeLimitIsValid(RoundSettings round)
    {
        if (round.TimeLimitSeconds < GameRules.MinimumTimeLimitSeconds || round.TimeLimitSeconds > GameRules.MaximumTimeLimitSeconds)
            throw new DomainException(
                $"El tiempo de la ronda {round.RoundNumber} debe estar entre {GameRules.MinimumTimeLimitSeconds} y {GameRules.MaximumTimeLimitSeconds} segundos.");
    }

    /// <summary>
    /// Business rule: every round pays strictly more than the previous one, so round 5 holds the top prize.
    /// </summary>
    /// <param name="orderedRounds">IReadOnlyList&lt;RoundSettings&gt;: settings ordered by round number.</param>
    private static void EnsurePrizesIncrease(IReadOnlyList<RoundSettings> orderedRounds)
    {
        for (var index = 1; index < orderedRounds.Count; index++)
        {
            var previousRound = orderedRounds[index - 1];
            var currentRound = orderedRounds[index];
            if (currentRound.Prize <= previousRound.Prize)
                throw new DomainException(
                    $"El premio de la ronda {currentRound.RoundNumber} debe ser mayor que el de la ronda {previousRound.RoundNumber}.");
        }
    }
}
