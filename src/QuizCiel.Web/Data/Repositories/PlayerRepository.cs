using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Data access for players.
/// </summary>
public interface IPlayerRepository
{
    /// <summary>
    /// Finds the tracked player that belongs to a login account.
    /// </summary>
    /// <param name="userId">string: login account identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Player?&gt;: the player or null (for example, the administrator has no player).</returns>
    Task<Player?> FindByUserIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a cédula is already registered.
    /// </summary>
    /// <param name="documentNumber">string: normalized cédula.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;bool&gt;: true when another player already uses it.</returns>
    Task<bool> IsDocumentRegisteredAsync(string documentNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a new player for insertion.
    /// </summary>
    /// <param name="player">Player: player to insert.</param>
    void Add(Player player);

    /// <summary>
    /// Runs several saves (for example, the login account and the player) as one database transaction.
    /// </summary>
    /// <param name="transactionalWork">Func&lt;Task&gt;: work to run; any exception rolls everything back.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when the transaction is committed.</returns>
    Task RunInTransactionAsync(Func<Task> transactionalWork, CancellationToken cancellationToken);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// EF Core implementation of <see cref="IPlayerRepository"/>.
/// </summary>
public class PlayerRepository : RepositoryBase, IPlayerRepository
{
    /// <summary>
    /// Creates the repository over the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: request-scoped context.</param>
    public PlayerRepository(QuizDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<Player?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return DbContext.Players.SingleOrDefaultAsync(player => player.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsDocumentRegisteredAsync(string documentNumber, CancellationToken cancellationToken)
    {
        return DbContext.Players.AnyAsync(player => player.DocumentNumber == documentNumber, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Player player)
    {
        DbContext.Players.Add(player);
    }

    /// <inheritdoc />
    public async Task RunInTransactionAsync(Func<Task> transactionalWork, CancellationToken cancellationToken)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        await transactionalWork();
        await transaction.CommitAsync(cancellationToken);
    }
}
