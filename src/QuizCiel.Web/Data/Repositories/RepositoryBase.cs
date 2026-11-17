namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Shared plumbing of every repository: access to the request-scoped context and persistence of changes.
/// </summary>
public abstract class RepositoryBase
{
    protected QuizDbContext DbContext { get; }

    /// <summary>
    /// Stores the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: context shared by all repositories of the request.</param>
    protected RepositoryBase(QuizDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// Persists every pending change of the request in one transaction.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when the changes are saved.</returns>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
