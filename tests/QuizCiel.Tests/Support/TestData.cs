using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Tests.Support;

/// <summary>
/// Builds valid domain objects for unit tests, assigning the identifiers the database would normally generate.
/// </summary>
internal static class TestData
{
    public static readonly DateTime GameStartUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly decimal[] PrizesByRound = [100_000m, 500_000m, 2_000_000m, 10_000_000m, 50_000_000m];
    public const int TimeLimitSeconds = 30;

    private static int lastGeneratedId = 1000;

    /// <summary>
    /// Builds four valid option drafts where the first one is correct.
    /// </summary>
    /// <param name="questionLabel">string: text used to make option texts unique.</param>
    /// <returns>List&lt;AnswerOptionDraft&gt;: four drafts with exactly one correct.</returns>
    public static List<AnswerOptionDraft> ValidOptionDrafts(string questionLabel = "Q")
    {
        return
        [
            new AnswerOptionDraft($"{questionLabel} correcta", true),
            new AnswerOptionDraft($"{questionLabel} incorrecta 1", false),
            new AnswerOptionDraft($"{questionLabel} incorrecta 2", false),
            new AnswerOptionDraft($"{questionLabel} incorrecta 3", false)
        ];
    }

    /// <summary>
    /// Builds an active question of the given level with identifiers and category assigned.
    /// </summary>
    /// <param name="level">int: category level between 1 and 5.</param>
    /// <returns>Question: question whose first option is the correct one.</returns>
    public static Question QuestionForLevel(int level)
    {
        var category = Category.Create($"Nivel {level}", level, null);
        SetPrivateProperty(category, nameof(Category.Id), level);

        var question = Question.Create(category.Id, $"Pregunta de nivel {level}", ValidOptionDrafts($"N{level}"));
        SetPrivateProperty(question, nameof(Question.Id), NextId());
        SetPrivateProperty(question, nameof(Question.Category), category);
        foreach (var option in question.Options)
            SetPrivateProperty(option, nameof(AnswerOption.Id), NextId());
        return question;
    }

    /// <summary>
    /// Builds the settings of a round using the standard test prizes.
    /// </summary>
    /// <param name="roundNumber">int: round number between 1 and 5.</param>
    /// <returns>RoundConfig: round configuration.</returns>
    public static RoundConfig RoundConfigFor(int roundNumber)
    {
        return RoundConfig.Create(new RoundSettings(roundNumber, PrizesByRound[roundNumber - 1], TimeLimitSeconds));
    }

    /// <summary>
    /// Builds valid settings for all rounds using the standard test prizes.
    /// </summary>
    /// <returns>List&lt;RoundSettings&gt;: settings for rounds 1 to 5.</returns>
    public static List<RoundSettings> ValidRoundSettings()
    {
        return Enumerable.Range(1, GameRules.TotalRounds)
            .Select(roundNumber => new RoundSettings(roundNumber, PrizesByRound[roundNumber - 1], TimeLimitSeconds))
            .ToList();
    }

    /// <summary>
    /// Starts a game in round 1 at <see cref="GameStartUtc"/>.
    /// </summary>
    /// <returns>Game: game in progress with its first question assigned.</returns>
    public static Game StartedGame()
    {
        var player = Player.Create("user-ana", "1012345678", "Ana Gómez", GameStartUtc);
        return Game.Start(player, RoundConfigFor(1), QuestionForLevel(1), GameStartUtc);
    }

    /// <summary>
    /// Answers the current question correctly and, if the game continues, assigns the next round question.
    /// </summary>
    /// <param name="game">Game: game in progress.</param>
    /// <param name="answeredAtUtc">DateTime: instant of the answer, in UTC.</param>
    public static void AnswerCorrectlyAndContinue(Game game, DateTime answeredAtUtc)
    {
        var correctOptionId = game.CurrentRound.Question.GetCorrectOption().Id;
        game.Answer(correctOptionId, answeredAtUtc);
        if (!game.IsFinished)
            game.AssignQuestion(RoundConfigFor(game.CurrentRoundNumber), QuestionForLevel(game.CurrentRoundNumber), answeredAtUtc);
    }

    /// <summary>
    /// Returns the identifier of any wrong option of the current question.
    /// </summary>
    /// <param name="game">Game: game in progress.</param>
    /// <returns>int: identifier of a wrong option.</returns>
    public static int WrongOptionIdOfCurrentRound(Game game)
    {
        return game.CurrentRound.Question.Options.First(option => !option.IsCorrect).Id;
    }

    /// <summary>
    /// Sets a property that has a private setter, emulating values assigned by the ORM.
    /// </summary>
    /// <param name="target">object: instance to modify.</param>
    /// <param name="propertyName">string: name of the property.</param>
    /// <param name="value">object: value to assign.</param>
    public static void SetPrivateProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);
    }

    /// <summary>
    /// Generates a unique identifier for test entities.
    /// </summary>
    /// <returns>int: a new identifier.</returns>
    private static int NextId()
    {
        return Interlocked.Increment(ref lastGeneratedId);
    }
}
