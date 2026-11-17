using QuizCiel.Tests.Support;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Tests.Services;

/// <summary>
/// Game use cases running against a real (SQLite in-memory) database through the ORM.
/// </summary>
public class GameServiceTests
{
    private const string AnaDocument = "1012345678";
    private const string OtherDanielDocument = "79123456";

    /// <summary>
    /// Starting a game persists it in round 1 with a level 1 question.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenConfigured_PersistsGameInFirstRound()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");

        var gameId = await StartGameAsync(database, anaUserId);

        await using var readContext = database.CreateContext();
        var storedGame = await database.CreateGameService(readContext).GetForViewerAsync(gameId, anaUserId, viewerIsAdmin: false, CancellationToken.None);
        Assert.Equal(GameStatus.InProgress, storedGame.Status);
        Assert.Equal(1, storedGame.CurrentRoundNumber);
        var askedQuestionLevel = readContext.Questions
            .Where(question => question.Id == storedGame.CurrentRound.QuestionId)
            .Select(question => question.Category.Level)
            .Single();
        Assert.Equal(1, askedQuestionLevel);
    }

    /// <summary>
    /// A game cannot start while a level has fewer active questions than required.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenLevelsLackQuestions_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync(questionsPerLevel: GameRules.MinimumActiveQuestionsPerCategory - 1);
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");

        await Assert.ThrowsAsync<DomainException>(() => StartGameAsync(database, anaUserId));
    }

    /// <summary>
    /// Each person takes part only once, even after losing.
    /// </summary>
    [Fact]
    public async Task StartAsync_SecondTime_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var firstGameId = await StartGameAsync(database, anaUserId);
        await AnswerCurrentRoundAsync(database, firstGameId, anaUserId, answerCorrectly: false);

        await Assert.ThrowsAsync<DomainException>(() => StartGameAsync(database, anaUserId));
    }

    /// <summary>
    /// Two people with the same name are different players and each can play.
    /// </summary>
    [Fact]
    public async Task StartAsync_TwoPlayersWithSameName_AreIndependent()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var firstDanielUserId = await database.CreatePlayerAsync("1098765432", "Daniel Pérez");
        var secondDanielUserId = await database.CreatePlayerAsync(OtherDanielDocument, "Daniel Pérez");

        await StartGameAsync(database, firstDanielUserId);
        await StartGameAsync(database, secondDanielUserId);

        await using var readContext = database.CreateContext();
        Assert.Equal(2, readContext.Games.Count());
    }

    /// <summary>
    /// An account that is not a player (for example the administrator) cannot start a game.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithoutPlayer_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();

        await Assert.ThrowsAsync<DomainException>(() => StartGameAsync(database, "account-without-player"));
    }

    /// <summary>
    /// Answering every round correctly, one request at a time, wins the sum of all prizes.
    /// </summary>
    [Fact]
    public async Task AnswerAsync_AllRoundsCorrect_PersistsWonGame()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var gameId = await StartGameAsync(database, anaUserId);

        for (var roundNumber = 1; roundNumber <= GameRules.TotalRounds; roundNumber++)
            await AnswerCurrentRoundAsync(database, gameId, anaUserId, answerCorrectly: true);

        await using var readContext = database.CreateContext();
        var storedGame = await database.CreateGameService(readContext).GetForViewerAsync(gameId, anaUserId, viewerIsAdmin: false, CancellationToken.None);
        Assert.Equal(GameStatus.Won, storedGame.Status);
        Assert.Equal(TestData.PrizesByRound.Sum(), storedGame.FinalPrize);
        Assert.Equal(GameRules.TotalRounds, storedGame.Rounds.Select(round => round.QuestionId).Distinct().Count());
    }

    /// <summary>
    /// A wrong answer persists a lost game without prize.
    /// </summary>
    [Fact]
    public async Task AnswerAsync_Wrong_PersistsLostGame()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var gameId = await StartGameAsync(database, anaUserId);
        await AnswerCurrentRoundAsync(database, gameId, anaUserId, answerCorrectly: true);

        var answerOutcome = await AnswerCurrentRoundAsync(database, gameId, anaUserId, answerCorrectly: false);

        await using var readContext = database.CreateContext();
        Assert.Equal(AnswerOutcome.Wrong, answerOutcome);
        Assert.Equal(GameStatus.Lost, readContext.Games.Single().Status);
        Assert.Equal(0m, readContext.Games.Single().FinalPrize);
    }

    /// <summary>
    /// Another player can neither see nor play someone else's game; an administrator can see it.
    /// </summary>
    [Fact]
    public async Task GameOfAnotherPlayer_IsHiddenFromPlayersButVisibleToAdmin()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var intruderUserId = await database.CreatePlayerAsync(OtherDanielDocument, "Daniel Pérez");
        var anaGameId = await StartGameAsync(database, anaUserId);

        await using var requestContext = database.CreateContext();
        var gameService = database.CreateGameService(requestContext);

        await Assert.ThrowsAsync<NotFoundException>(() => gameService.GetForViewerAsync(anaGameId, intruderUserId, viewerIsAdmin: false, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => gameService.WithdrawAsync(anaGameId, intruderUserId, CancellationToken.None));
        var gameSeenByAdmin = await gameService.GetForViewerAsync(anaGameId, "admin-account", viewerIsAdmin: true, CancellationToken.None);
        Assert.Equal(anaGameId, gameSeenByAdmin.Id);
    }

    /// <summary>
    /// The prizes list contains only finished games with a prize, with the player's cédula.
    /// </summary>
    [Fact]
    public async Task GetPrizesToPay_ListsOnlyGamesWithPrize()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var danielUserId = await database.CreatePlayerAsync(OtherDanielDocument, "Daniel Pérez");
        var anaGameId = await StartGameAsync(database, anaUserId);
        await AnswerCurrentRoundAsync(database, anaGameId, anaUserId, answerCorrectly: true);
        await WithdrawAsync(database, anaGameId, anaUserId);
        var danielGameId = await StartGameAsync(database, danielUserId);
        await AnswerCurrentRoundAsync(database, danielGameId, danielUserId, answerCorrectly: false);

        await using var readContext = database.CreateContext();
        var prizesToPay = await database.CreateGameService(readContext).GetPrizesToPayAsync(CancellationToken.None);

        var anaPrize = Assert.Single(prizesToPay);
        Assert.Equal(AnaDocument, anaPrize.PlayerDocumentNumber);
        Assert.Equal(TestData.PrizesByRound[0], anaPrize.FinalPrize);
    }

    /// <summary>
    /// The participation of a player whose time ran out shows the forced end without prize.
    /// </summary>
    [Fact]
    public async Task GetParticipationAsync_AfterTimeLimit_ShowsForcedEnd()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        await StartGameAsync(database, anaUserId);
        database.Clock.Advance(TimeSpan.FromMinutes(5));

        await using var requestContext = database.CreateContext();
        var participation = await database.CreateGameService(requestContext).GetParticipationAsync(anaUserId, CancellationToken.None);

        Assert.Equal(GameStatus.ForcedEnd, participation.Game!.Status);
        Assert.Equal(0m, participation.Game.FinalPrize);
    }

    /// <summary>
    /// A game abandoned after its time ran out is closed when the administrator lists the games, so it is not
    /// shown as "in progress" forever.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_AbandonedGameAfterTimeLimit_IsListedAsForcedEnd()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        await StartGameAsync(database, anaUserId);
        database.Clock.Advance(TimeSpan.FromMinutes(5));

        await using var adminContext = database.CreateContext();
        var history = await database.CreateGameService(adminContext).GetHistoryAsync(null, CancellationToken.None);

        var abandonedGame = Assert.Single(history);
        Assert.Equal(GameStatus.ForcedEnd, abandonedGame.Status);
        Assert.Equal(0m, abandonedGame.FinalPrize);
    }

    /// <summary>
    /// Listing the games does not close a game whose time is still running.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_GameWithinTimeLimit_StaysInProgress()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        await StartGameAsync(database, anaUserId);
        database.Clock.Advance(TimeSpan.FromSeconds(TestData.TimeLimitSeconds - 1));

        await using var adminContext = database.CreateContext();
        var history = await database.CreateGameService(adminContext).GetHistoryAsync(null, CancellationToken.None);

        Assert.Equal(GameStatus.InProgress, Assert.Single(history).Status);
    }

    /// <summary>
    /// Two tabs open an expired game at the same moment: the second one must show the forced end instead of
    /// failing with a concurrency error, because viewing a game is not an action of the player.
    /// </summary>
    [Fact]
    public async Task GetForViewerAsync_ExpiredGameOpenedTwiceAtOnce_ShowsForcedEndInBothTabs()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var gameId = await StartGameAsync(database, anaUserId);

        await using var firstTabContext = database.CreateContext();
        await using var secondTabContext = database.CreateContext();
        var secondTabService = database.CreateGameService(secondTabContext);
        await secondTabService.GetForViewerAsync(gameId, anaUserId, false, CancellationToken.None);
        database.Clock.Advance(TimeSpan.FromMinutes(5));

        var gameInFirstTab = await database.CreateGameService(firstTabContext).GetForViewerAsync(gameId, anaUserId, false, CancellationToken.None);
        var gameInSecondTab = await secondTabService.GetForViewerAsync(gameId, anaUserId, false, CancellationToken.None);

        Assert.Equal(GameStatus.ForcedEnd, gameInFirstTab.Status);
        Assert.Equal(GameStatus.ForcedEnd, gameInSecondTab.Status);
    }

    /// <summary>
    /// Two tabs load the same game; one answers and then the other, still holding the old state, tries to
    /// withdraw. The stale request is rejected instead of overwriting the answer.
    /// </summary>
    [Fact]
    public async Task WithdrawAsync_OnStaleGame_IsRejectedAsConcurrencyConflict()
    {
        using var database = new SqliteTestDatabase();
        await database.SeedPlayableGameAsync();
        var anaUserId = await database.CreatePlayerAsync(AnaDocument, "Ana Gómez");
        var gameId = await StartGameAsync(database, anaUserId);

        await using var answeringTabContext = database.CreateContext();
        await using var staleTabContext = database.CreateContext();
        var answeringTabService = database.CreateGameService(answeringTabContext);
        var staleTabService = database.CreateGameService(staleTabContext);
        var gameSeenByAnsweringTab = await answeringTabService.GetForViewerAsync(gameId, anaUserId, false, CancellationToken.None);
        await staleTabService.GetForViewerAsync(gameId, anaUserId, false, CancellationToken.None);

        var correctOptionId = gameSeenByAnsweringTab.CurrentRound.Question.GetCorrectOption().Id;
        await answeringTabService.AnswerAsync(gameId, anaUserId, correctOptionId, CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            staleTabService.WithdrawAsync(gameId, anaUserId, CancellationToken.None));
    }

    /// <summary>
    /// Starts a game in its own simulated request.
    /// </summary>
    /// <param name="database">SqliteTestDatabase: test database.</param>
    /// <param name="userId">string: login account of the player.</param>
    /// <returns>Task&lt;Guid&gt;: new game identifier.</returns>
    private static async Task<Guid> StartGameAsync(SqliteTestDatabase database, string userId)
    {
        await using var requestContext = database.CreateContext();
        return await database.CreateGameService(requestContext).StartAsync(userId, CancellationToken.None);
    }

    /// <summary>
    /// Withdraws from a game in its own simulated request.
    /// </summary>
    /// <param name="database">SqliteTestDatabase: test database.</param>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the player.</param>
    /// <returns>Task: completes when saved.</returns>
    private static async Task WithdrawAsync(SqliteTestDatabase database, Guid gameId, string userId)
    {
        await using var requestContext = database.CreateContext();
        await database.CreateGameService(requestContext).WithdrawAsync(gameId, userId, CancellationToken.None);
    }

    /// <summary>
    /// Answers the current round in its own simulated request.
    /// </summary>
    /// <param name="database">SqliteTestDatabase: test database.</param>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the player.</param>
    /// <param name="answerCorrectly">bool: whether to choose the correct option.</param>
    /// <returns>Task&lt;AnswerOutcome&gt;: outcome of the answer.</returns>
    private static async Task<AnswerOutcome> AnswerCurrentRoundAsync(SqliteTestDatabase database, Guid gameId, string userId, bool answerCorrectly)
    {
        await using var requestContext = database.CreateContext();
        var gameService = database.CreateGameService(requestContext);
        var game = await gameService.GetForViewerAsync(gameId, userId, viewerIsAdmin: false, CancellationToken.None);
        var chosenOption = game.CurrentRound.Question.Options.First(option => option.IsCorrect == answerCorrectly);
        return await gameService.AnswerAsync(gameId, userId, chosenOption.Id, CancellationToken.None);
    }
}
