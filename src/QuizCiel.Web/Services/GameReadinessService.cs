using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Services;

/// <summary>
/// Readiness of one level: whether it has enough active questions to be played.
/// </summary>
/// <param name="Level">int: level (and round) number.</param>
/// <param name="ActiveQuestionCount">int: active questions available for the level.</param>
/// <param name="IsReady">bool: true when the level meets the minimum.</param>
public record LevelReadiness(int Level, int ActiveQuestionCount, bool IsReady);

/// <summary>
/// Result of checking whether a game can be started.
/// </summary>
/// <param name="Levels">IReadOnlyList&lt;LevelReadiness&gt;: readiness of each level.</param>
/// <param name="Issues">IReadOnlyList&lt;string&gt;: user-facing problems; empty when ready.</param>
public record GameReadinessReport(IReadOnlyList<LevelReadiness> Levels, IReadOnlyList<string> Issues)
{
    public bool IsReady => Issues.Count == 0;
}

/// <summary>
/// Checks the business rules that must hold before anyone can start a game.
/// </summary>
public class GameReadinessService
{
    private readonly IQuestionRepository questionRepository;
    private readonly IRoundConfigRepository roundConfigRepository;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="questionRepository">IQuestionRepository: source of question counts.</param>
    /// <param name="roundConfigRepository">IRoundConfigRepository: source of round settings.</param>
    public GameReadinessService(IQuestionRepository questionRepository, IRoundConfigRepository roundConfigRepository)
    {
        this.questionRepository = questionRepository;
        this.roundConfigRepository = roundConfigRepository;
    }

    /// <summary>
    /// Business rules: every level needs at least the minimum of active questions and the round settings must be valid.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the queries.</param>
    /// <returns>Task&lt;GameReadinessReport&gt;: readiness per level and the list of problems.</returns>
    public async Task<GameReadinessReport> GetReportAsync(CancellationToken cancellationToken)
    {
        var activeCountByLevel = await questionRepository.CountActiveByLevelAsync(cancellationToken);
        var levelReadiness = Enumerable.Range(GameRules.MinimumLevel, GameRules.TotalRounds)
            .Select(level => BuildLevelReadiness(level, activeCountByLevel))
            .ToList();

        var issues = levelReadiness
            .Where(level => !level.IsReady)
            .Select(level => $"El nivel {level.Level} tiene {level.ActiveQuestionCount} preguntas activas; se requieren al menos {GameRules.MinimumActiveQuestionsPerCategory}.")
            .ToList();

        var roundSettingsIssue = await ValidateRoundSettingsAsync(cancellationToken);
        if (roundSettingsIssue is not null)
            issues.Add(roundSettingsIssue);

        return new GameReadinessReport(levelReadiness, issues);
    }

    /// <summary>
    /// Throws when the game cannot be started.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the queries.</param>
    /// <returns>Task: completes when the game is ready.</returns>
    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        var readinessReport = await GetReportAsync(cancellationToken);
        if (!readinessReport.IsReady)
            throw new DomainException("El juego aún no está configurado. Pide a un administrador que lo complete.");
    }

    /// <summary>
    /// Builds the readiness of one level from the active question counts.
    /// </summary>
    /// <param name="level">int: level number.</param>
    /// <param name="activeCountByLevel">IReadOnlyDictionary&lt;int, int&gt;: active questions per level.</param>
    /// <returns>LevelReadiness: readiness of the level.</returns>
    private static LevelReadiness BuildLevelReadiness(int level, IReadOnlyDictionary<int, int> activeCountByLevel)
    {
        var activeQuestionCount = activeCountByLevel.GetValueOrDefault(level);
        return new LevelReadiness(level, activeQuestionCount, activeQuestionCount >= GameRules.MinimumActiveQuestionsPerCategory);
    }

    /// <summary>
    /// Validates the stored round settings as a whole.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;string?&gt;: the problem found, or null when the settings are valid.</returns>
    private async Task<string?> ValidateRoundSettingsAsync(CancellationToken cancellationToken)
    {
        var storedRounds = await roundConfigRepository.GetAllAsync(cancellationToken);
        try
        {
            GameConfiguration.Create(storedRounds.Select(round => round.ToSettings()));
            return null;
        }
        catch (DomainException invalidSettings)
        {
            return $"Configuración de rondas inválida: {invalidSettings.Message}";
        }
    }
}
