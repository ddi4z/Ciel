using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Data access for the round settings.
/// </summary>
public interface IRoundConfigRepository
{
    /// <summary>
    /// Lists the tracked settings of every configured round, ordered by round number.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;RoundConfig&gt;&gt;: configured rounds.</returns>
    Task<List<RoundConfig>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds the settings of one round.
    /// </summary>
    /// <param name="roundNumber">int: round number.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;RoundConfig?&gt;: the settings or null.</returns>
    Task<RoundConfig?> FindAsync(int roundNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Marks new round settings for insertion.
    /// </summary>
    /// <param name="roundConfig">RoundConfig: settings to insert.</param>
    void Add(RoundConfig roundConfig);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// EF Core implementation of <see cref="IRoundConfigRepository"/>.
/// </summary>
public class RoundConfigRepository : RepositoryBase, IRoundConfigRepository
{
    /// <summary>
    /// Creates the repository over the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: request-scoped context.</param>
    public RoundConfigRepository(QuizDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<List<RoundConfig>> GetAllAsync(CancellationToken cancellationToken)
    {
        return DbContext.RoundConfigs.OrderBy(roundConfig => roundConfig.RoundNumber).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<RoundConfig?> FindAsync(int roundNumber, CancellationToken cancellationToken)
    {
        return DbContext.RoundConfigs.SingleOrDefaultAsync(roundConfig => roundConfig.RoundNumber == roundNumber, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(RoundConfig roundConfig)
    {
        DbContext.RoundConfigs.Add(roundConfig);
    }
}
