using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuizCiel.Web.Data;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Abstractions;
using QuizCiel.Web.Services.Strategies;

namespace QuizCiel.Tests.Support;

/// <summary>
/// In-memory SQLite database created from the EF Core model. Each test gets its own instance, and each
/// simulated request gets its own context, like the scoped context of the web application.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public FakeClock Clock { get; } = new(TestData.GameStartUtc);

    public SqliteConnection Connection => connection;

    /// <summary>
    /// Opens the connection and creates the schema.
    /// </summary>
    public SqliteTestDatabase()
    {
        connection.Open();
        using var schemaContext = CreateContext();
        schemaContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new context over the shared in-memory database (one per simulated request).
    /// </summary>
    /// <returns>QuizDbContext: new context.</returns>
    public QuizDbContext CreateContext()
    {
        var contextOptions = new DbContextOptionsBuilder<QuizDbContext>().UseSqlite(connection).Options;
        return new QuizDbContext(contextOptions);
    }

    /// <summary>
    /// Inserts one category per level with the given number of questions and valid round settings.
    /// </summary>
    /// <param name="questionsPerLevel">int: active questions created in every level.</param>
    /// <returns>Task: completes when the data is saved.</returns>
    public async Task SeedPlayableGameAsync(int questionsPerLevel = GameRules.MinimumActiveQuestionsPerCategory)
    {
        await using var seedContext = CreateContext();
        for (var level = GameRules.MinimumLevel; level <= GameRules.MaximumLevel; level++)
        {
            var category = Category.Create($"Categoría {level}", level, null);
            seedContext.Categories.Add(category);
            await seedContext.SaveChangesAsync();

            for (var questionNumber = 1; questionNumber <= questionsPerLevel; questionNumber++)
                seedContext.Questions.Add(Question.Create(category.Id, $"Pregunta {level}.{questionNumber}", TestData.ValidOptionDrafts($"{level}.{questionNumber}")));
        }

        foreach (var roundSettings in TestData.ValidRoundSettings())
            seedContext.RoundConfigs.Add(RoundConfig.Create(roundSettings));
        await seedContext.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts a login account and its player, emulating a completed registration.
    /// </summary>
    /// <param name="documentNumber">string: cédula of the player.</param>
    /// <param name="fullName">string: full name of the player.</param>
    /// <returns>Task&lt;string&gt;: identifier of the login account.</returns>
    public async Task<string> CreatePlayerAsync(string documentNumber, string fullName)
    {
        await using var registrationContext = CreateContext();
        var playerAccount = new IdentityUser(documentNumber);
        registrationContext.Users.Add(playerAccount);
        registrationContext.Players.Add(Player.Create(playerAccount.Id, documentNumber, fullName, Clock.GetUtcNow()));
        await registrationContext.SaveChangesAsync();
        return playerAccount.Id;
    }

    /// <summary>
    /// Builds the game service exactly as dependency injection would, over the given context.
    /// </summary>
    /// <param name="requestContext">QuizDbContext: context of the simulated request.</param>
    /// <returns>GameService: service ready to use.</returns>
    public GameService CreateGameService(QuizDbContext requestContext)
    {
        var questionRepository = new QuestionRepository(requestContext);
        var roundConfigRepository = new RoundConfigRepository(requestContext);
        var deterministicSelector = new RandomQuestionSelector(questionRepository, new Random(42));
        var readinessService = new GameReadinessService(questionRepository, roundConfigRepository);
        return new GameService(
            new GameRepository(requestContext),
            new PlayerRepository(requestContext),
            roundConfigRepository,
            deterministicSelector,
            readinessService,
            Clock,
            NullLogger<GameService>.Instance);
    }

    /// <summary>
    /// Closes the in-memory database.
    /// </summary>
    public void Dispose()
    {
        connection.Dispose();
    }
}

/// <summary>
/// Controllable clock for time-dependent rules.
/// </summary>
internal sealed class FakeClock : IClock
{
    private DateTime currentUtc;

    /// <summary>
    /// Creates the clock at a fixed instant.
    /// </summary>
    /// <param name="initialUtc">DateTime: starting instant in UTC.</param>
    public FakeClock(DateTime initialUtc)
    {
        currentUtc = initialUtc;
    }

    /// <inheritdoc />
    public DateTime GetUtcNow()
    {
        return currentUtc;
    }

    /// <summary>
    /// Moves the clock forward.
    /// </summary>
    /// <param name="elapsedTime">TimeSpan: time to add.</param>
    public void Advance(TimeSpan elapsedTime)
    {
        currentUtc = currentUtc.Add(elapsedTime);
    }
}
