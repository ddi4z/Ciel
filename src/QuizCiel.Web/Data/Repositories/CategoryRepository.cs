using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Repositories;

/// <summary>
/// Data access for categories.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Lists every category with its question counts, ordered by level.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;CategorySummary&gt;&gt;: category rows.</returns>
    Task<List<CategorySummary>> GetSummariesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds a tracked category by identifier.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Category?&gt;: the category or null.</returns>
    Task<Category?> FindAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether another category already uses a level.
    /// </summary>
    /// <param name="level">int: level to check.</param>
    /// <param name="excludedCategoryId">int?: category to ignore (the one being edited).</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;bool&gt;: true when the level is taken.</returns>
    Task<bool> IsLevelTakenAsync(int level, int? excludedCategoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Counts all questions (active or not) of a category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;int&gt;: number of questions.</returns>
    Task<int> CountQuestionsAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a new category for insertion.
    /// </summary>
    /// <param name="category">Category: category to insert.</param>
    void Add(Category category);

    /// <summary>
    /// Marks a category for deletion.
    /// </summary>
    /// <param name="category">Category: category to delete.</param>
    void Remove(Category category);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/>.
/// </summary>
public class CategoryRepository : RepositoryBase, ICategoryRepository
{
    /// <summary>
    /// Creates the repository over the request-scoped context.
    /// </summary>
    /// <param name="dbContext">QuizDbContext: request-scoped context.</param>
    public CategoryRepository(QuizDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<List<CategorySummary>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        return DbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Level)
            .Select(category => new CategorySummary(
                category.Id,
                category.Name,
                category.Level,
                category.Description,
                category.Questions.Count(question => question.IsActive),
                category.Questions.Count))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Category?> FindAsync(int categoryId, CancellationToken cancellationToken)
    {
        return DbContext.Categories.SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsLevelTakenAsync(int level, int? excludedCategoryId, CancellationToken cancellationToken)
    {
        return DbContext.Categories.AnyAsync(
            category => category.Level == level && category.Id != excludedCategoryId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountQuestionsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return DbContext.Questions.CountAsync(question => question.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Category category)
    {
        DbContext.Categories.Add(category);
    }

    /// <inheritdoc />
    public void Remove(Category category)
    {
        DbContext.Categories.Remove(category);
    }
}
