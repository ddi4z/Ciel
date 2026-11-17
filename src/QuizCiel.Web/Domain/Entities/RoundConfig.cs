namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// Persisted settings of one game round: the prize for answering it and the time available.
/// The round number also selects the category level that supplies its questions.
/// </summary>
public class RoundConfig
{
    public int RoundNumber { get; private set; }
    public decimal Prize { get; private set; }
    public int TimeLimitSeconds { get; private set; }

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private RoundConfig()
    {
    }

    /// <summary>
    /// Creates the persisted settings of a round from settings already validated by <see cref="GameConfiguration"/>.
    /// </summary>
    /// <param name="settings">RoundSettings: validated round values.</param>
    /// <returns>RoundConfig: the new round configuration.</returns>
    public static RoundConfig Create(RoundSettings settings)
    {
        var roundConfig = new RoundConfig { RoundNumber = settings.RoundNumber };
        roundConfig.Apply(settings);
        return roundConfig;
    }

    /// <summary>
    /// Overwrites prize and time limit with settings already validated by <see cref="GameConfiguration"/>.
    /// </summary>
    /// <param name="settings">RoundSettings: validated round values for this same round number.</param>
    public void Apply(RoundSettings settings)
    {
        Prize = settings.Prize;
        TimeLimitSeconds = settings.TimeLimitSeconds;
    }

    /// <summary>
    /// Returns the stored values as input for <see cref="GameConfiguration"/>.
    /// </summary>
    /// <returns>RoundSettings: prize and time limit of this round.</returns>
    public RoundSettings ToSettings()
    {
        return new RoundSettings(RoundNumber, Prize, TimeLimitSeconds);
    }
}

/// <summary>
/// Input values of one round, validated as a whole by <see cref="GameConfiguration"/>.
/// </summary>
/// <param name="RoundNumber">int: round number between 1 and 5.</param>
/// <param name="Prize">decimal: prize in whole Colombian pesos.</param>
/// <param name="TimeLimitSeconds">int: seconds available to answer.</param>
public record RoundSettings(int RoundNumber, decimal Prize, int TimeLimitSeconds);
