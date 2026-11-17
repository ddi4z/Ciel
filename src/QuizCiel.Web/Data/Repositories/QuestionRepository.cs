using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Data access for questions and their options.
/// </summary>
public interface IQuestionRepository
{
    /// <summary>
    /// Lists the questions of a category for administration.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;QuestionSummary&gt;&gt;: question rows, active first.</returns>
    Task<List<QuestionSummary>> GetSummariesByCategoryAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds a tracked question with its category and options.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Question?&gt;: the question or null.</returns>
    Task<Question?> FindWithOptionsAsync(int questionId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the identifiers of the active questions whose category has the given level.
    /// </summary>
    /// <param name="level">int: category level.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;int&gt;&gt;: candidate question identifiers.</returns>
    Task<List<int>> GetActiveQuestionIdsByLevelAsync(int level, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the active questions of a category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;int&gt;: number of active questions.</returns>
    Task<int> CountActiveInCategoryAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Counts active questions grouped by category level.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Dictionary&lt;int, int&gt;&gt;: level to number of active questions.</returns>
    Task<Dictionary<int, int>> CountActiveByLevelAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether any game round already used a question.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;bool&gt;: true when the question was played.</returns>
    Task<bool> HasBeenPlayedAsync(int questionId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a new question for insertion.
    /// </summary>
    /// <param name="question">Question: question to insert.</param>
    void Add(Question question);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// EF Core implementation of <see cref="IQuestionRepository"/>.
/// </summary>
public class QuestionRepository : RepositoryBase, IQuestionRepository
{
    /// <summary>
    /// Creates the repository over the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: request-scoped context.</param>
    public QuestionRepository(QuizDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<List<QuestionSummary>> GetSummariesByCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return DbContext.Questions
            .AsNoTracking()
            .Where(question => question.CategoryId == categoryId)
            .OrderByDescending(question => question.IsActive)
            .ThenBy(question => question.Id)
            .Select(question => new QuestionSummary(
                question.Id,
                question.Text,
                question.IsActive,
                question.Options.Where(option => option.IsCorrect).Select(option => option.Text).First(),
                DbContext.GameRounds.Count(round => round.QuestionId == question.Id)))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Question?> FindWithOptionsAsync(int questionId, CancellationToken cancellationToken)
    {
        return DbContext.Questions
            .Include(question => question.Category)
            .Include(question => question.Options)
            .SingleOrDefaultAsync(question => question.Id == questionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<int>> GetActiveQuestionIdsByLevelAsync(int level, CancellationToken cancellationToken)
    {
        return DbContext.Questions
            .Where(question => question.IsActive && question.Category.Level == level)
            .Select(question => question.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountActiveInCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return DbContext.Questions.CountAsync(
            question => question.CategoryId == categoryId && question.IsActive,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Dictionary<int, int>> CountActiveByLevelAsync(CancellationToken cancellationToken)
    {
        return DbContext.Questions
            .Where(question => question.IsActive)
            .GroupBy(question => question.Category.Level)
            .Select(levelGroup => new { Level = levelGroup.Key, ActiveCount = levelGroup.Count() })
            .ToDictionaryAsync(levelCount => levelCount.Level, levelCount => levelCount.ActiveCount, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasBeenPlayedAsync(int questionId, CancellationToken cancellationToken)
    {
        return DbContext.GameRounds.AnyAsync(round => round.QuestionId == questionId, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Question question)
    {
        DbContext.Questions.Add(question);
    }
}
