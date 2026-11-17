using Microsoft.EntityFrameworkCore;
using QuizCiel.Tests.Support;
using QuizCiel.Web.Data;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Services;

namespace QuizCiel.Tests.Services;

/// <summary>
/// Administrator use cases running against a real (SQLite in-memory) database through the ORM.
/// </summary>
public class AdministrationServiceTests
{
    /// <summary>
    /// Saving rounds whose prizes do not increase is rejected and nothing is stored.
    /// </summary>
    [Fact]
    public async Task SaveRounds_WithDecreasingPrize_IsRejectedAndNotStored()
    {
        using var database = new SqliteTestDatabase();
        var roundSettings = TestData.ValidRoundSettings();
        roundSettings[4] = roundSettings[4] with { Prize = roundSettings[3].Prize - 1 };

        await using (var requestContext = database.CreateContext())
        {
            var roundSettingsService = new RoundSettingsService(new RoundConfigRepository(requestContext));
            await Assert.ThrowsAsync<DomainException>(() => roundSettingsService.SaveAsync(roundSettings, CancellationToken.None));
        }

        await using var readContext = database.CreateContext();
        Assert.Empty(readContext.RoundConfigs);
    }

    /// <summary>
    /// Saving valid rounds updates the existing ones.
    /// </summary>
    [Fact]
    public async Task SaveRounds_Valid_UpdatesStoredRounds()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var doubledSettings = TestData.ValidRoundSettings().Select(round => round with { Prize = round.Prize * 2 }).ToList();

        await using (var requestContext = database.CreateContext())
            await new RoundSettingsService(new RoundConfigRepository(requestContext)).SaveAsync(doubledSettings, CancellationToken.None);

        await using var readContext = database.CreateContext();
        Assert.Equal(TestData.PrizesByRound[4] * 2, readContext.RoundConfigs.Single(round => round.RoundNumber == 5).Prize);
    }

    /// <summary>
    /// A second category for the same level is rejected.
    /// </summary>
    [Fact]
    public async Task CreateCategory_WithTakenLevel_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();

        await using var requestContext = database.CreateContext();
        var categoryService = new CategoryService(new CategoryRepository(requestContext));

        await Assert.ThrowsAsync<DomainException>(() => categoryService.CreateAsync("Otra", 3, null, CancellationToken.None));
    }

    /// <summary>
    /// Deactivating a question is rejected when its category is at the minimum of active questions.
    /// </summary>
    [Fact]
    public async Task DeactivateQuestion_AtMinimum_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();

        await using var requestContext = database.CreateContext();
        var anyQuestionId = await requestContext.Questions.Select(question => question.Id).FirstAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateQuestionService(requestContext).DeactivateAsync(anyQuestionId, CancellationToken.None));
    }

    /// <summary>
    /// Editing a never played question can move the correct answer to a later or an earlier option
    /// without breaking the "single correct option" unique index.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    public async Task UpdateQuestion_MovingCorrectOption_IsPersisted(int newCorrectPosition)
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        await using (var moveToLastContext = database.CreateContext())
        {
            var firstQuestionId = await moveToLastContext.Questions.Select(question => question.Id).FirstAsync();
            var correctLastDrafts = TestData.ValidOptionDrafts("L").Select((draft, position) => draft with { IsCorrect = position == 3 }).ToList();
            await CreateQuestionService(moveToLastContext).UpdateAsync(firstQuestionId, "Previa", correctLastDrafts, CancellationToken.None);
        }
        var editedDrafts = TestData.ValidOptionDrafts("E").Select((draft, position) => draft with { IsCorrect = position == newCorrectPosition }).ToList();

        int questionId;
        await using (var requestContext = database.CreateContext())
        {
            questionId = await requestContext.Questions.Select(question => question.Id).FirstAsync();
            await CreateQuestionService(requestContext).UpdateAsync(questionId, "Editada", editedDrafts, CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var correctOptionText = readContext.AnswerOptions.Single(option => option.QuestionId == questionId && option.IsCorrect).Text;
        Assert.Equal(editedDrafts[newCorrectPosition].Text, correctOptionText);
    }

    /// <summary>
    /// A question already used in a game cannot be edited.
    /// </summary>
    [Fact]
    public async Task UpdateQuestion_AlreadyPlayed_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync("1012345678", "Ana Gómez");
        await using (var startContext = database.CreateContext())
            await database.CreateGameService(startContext).StartAsync(anaUserId, CancellationToken.None);

        await using var requestContext = database.CreateContext();
        var playedQuestionId = await requestContext.GameRounds.Select(round => round.QuestionId).FirstAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateQuestionService(requestContext).UpdateAsync(playedQuestionId, "Editada", TestData.ValidOptionDrafts(), CancellationToken.None));
    }

    /// <summary>
    /// The readiness report flags levels below the minimum of active questions.
    /// </summary>
    [Fact]
    public async Task Readiness_WithMissingQuestions_ReportsEveryLevel()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync(questionsPerLevel: 2);

        await using var requestContext = database.CreateContext();
        var readinessService = new GameReadinessService(new QuestionRepository(requestContext), new RoundConfigRepository(requestContext));
        var readinessReport = await readinessService.GetReportAsync(CancellationToken.None);

        Assert.False(readinessReport.IsReady);
        Assert.Equal(GameRules.TotalRounds, readinessReport.Issues.Count);
    }

    /// <summary>
    /// Builds the question service over a request context.
    /// </summary>
    /// <param name="requestContext">QuizDbContext: context of the simulated request.</param>
    /// <returns>QuestionService: service ready to use.</returns>
    private static QuestionService CreateQuestionService(QuizDbContext requestContext)
    {
        return new QuestionService(new QuestionRepository(requestContext), new CategoryRepository(requestContext));
    }
}
