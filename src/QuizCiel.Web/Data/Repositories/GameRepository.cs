using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;

namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Data access for games.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Loads a tracked game with its player, rounds, questions and options, ready to be played.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Game?&gt;: the game or null.</returns>
    Task<Game?> FindForPlayAsync(Guid gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the identifier of the game a player took part in.
    /// </summary>
    /// <param name="playerId">int: player identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Guid?&gt;: the game identifier, or null if the player has not played.</returns>
    Task<Guid?> FindGameIdOfPlayerAsync(int playerId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads, tracked and with their rounds, the games still in progress whose unanswered question was asked
    /// before the given instant. They are only candidates: the domain decides whether each one really expired.
    /// </summary>
    /// <param name="askedBeforeUtc">DateTime: instant in UTC; questions asked later cannot have expired yet.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;Game&gt;&gt;: candidate games.</returns>
    Task<List<Game>> FindPossiblyExpiredGamesAsync(DateTime askedBeforeUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Forgets every tracked change of the request, so the next query reads fresh data (used after a concurrency conflict).
    /// </summary>
    void DiscardPendingChanges();

    /// <summary>
    /// Lists the most recent games, optionally filtered by part of the player name or cédula.
    /// </summary>
    /// <param name="playerFilter">string?: text contained in the name or cédula, or null for all.</param>
    /// <param name="maximumResults">int: maximum number of rows.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;GameHistoryItem&gt;&gt;: games, newest first.</returns>
    Task<List<GameHistoryItem>> GetHistoryAsync(string? playerFilter, int maximumResults, CancellationToken cancellationToken);

    /// <summary>
    /// Lists every finished game whose player takes home a prize greater than zero.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;GameHistoryItem&gt;&gt;: games with prize, oldest first (payment order).</returns>
    Task<List<GameHistoryItem>> GetGamesWithPrizeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks a new game for insertion.
    /// </summary>
    /// <param name="game">Game: game to insert.</param>
    void Add(Game game);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// EF Core implementation of <see cref="IGameRepository"/>.
/// </summary>
public class GameRepository : RepositoryBase, IGameRepository
{
    /// <summary>
    /// Creates the repository over the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: request-scoped context.</param>
    public GameRepository(QuizDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<Game?> FindForPlayAsync(Guid gameId, CancellationToken cancellationToken)
    {
        return DbContext.Games
            .Include(game => game.Player)
            .Include(game => game.Rounds).ThenInclude(round => round.Question).ThenInclude(question => question.Options)
            .AsSplitQuery()
            .SingleOrDefaultAsync(game => game.Id == gameId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Guid?> FindGameIdOfPlayerAsync(int playerId, CancellationToken cancellationToken)
    {
        return DbContext.Games
            .Where(game => game.PlayerId == playerId)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Game>> FindPossiblyExpiredGamesAsync(DateTime askedBeforeUtc, CancellationToken cancellationToken)
    {
        return DbContext.Games
            .Include(game => game.Rounds)
            .Where(game => game.Status == GameStatus.InProgress
                && game.Rounds.Any(round => round.SelectedOptionId == null && round.AskedAt < askedBeforeUtc))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void DiscardPendingChanges()
    {
        DbContext.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public Task<List<GameHistoryItem>> GetHistoryAsync(string? playerFilter, int maximumResults, CancellationToken cancellationToken)
    {
        var gamesQuery = DbContext.Games.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(playerFilter))
        {
            var trimmedFilter = playerFilter.Trim();
            gamesQuery = gamesQuery.Where(game =>
                game.Player.FullName.Contains(trimmedFilter) || game.Player.DocumentNumber.Contains(trimmedFilter));
        }

        return ProjectToHistoryItems(gamesQuery.OrderByDescending(game => game.StartedAt).Take(maximumResults))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<GameHistoryItem>> GetGamesWithPrizeAsync(CancellationToken cancellationToken)
    {
        var gamesWithPrize = DbContext.Games
            .AsNoTracking()
            .Where(game => game.FinalPrize > 0)
            .OrderBy(game => game.EndedAt);
        return ProjectToHistoryItems(gamesWithPrize).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Game game)
    {
        DbContext.Games.Add(game);
    }

    /// <summary>
    /// Projects games into history rows in the database query (no entities are materialized).
    /// </summary>
    /// <param name="gamesQuery">IQueryable&lt;Game&gt;: already filtered and ordered games.</param>
    /// <returns>IQueryable&lt;GameHistoryItem&gt;: projected rows.</returns>
    private static IQueryable<GameHistoryItem> ProjectToHistoryItems(IQueryable<Game> gamesQuery)
    {
        return gamesQuery.Select(game => new GameHistoryItem(
            game.Id,
            game.Player.FullName,
            game.Player.DocumentNumber,
            game.Status,
            game.CurrentRoundNumber,
            game.AccumulatedPrize,
            game.FinalPrize,
            game.StartedAt,
            game.EndedAt));
    }
}
