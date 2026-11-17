using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Services;

/// <summary>
/// Service layer for the administrator's category use cases.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository categoryRepository;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="categoryRepository">ICategoryRepository: category data access.</param>
    public CategoryService(ICategoryRepository categoryRepository)
    {
        this.categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Lists every category with its question counts.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;CategorySummary&gt;&gt;: categories ordered by level.</returns>
    public Task<List<CategorySummary>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        return categoryRepository.GetSummariesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads one category or fails with a not found error.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Category&gt;: the category.</returns>
    public async Task<Category> GetAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await categoryRepository.FindAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("La categoría no existe.");
    }

    /// <summary>
    /// Creates a category. Business rule: only one category per level.
    /// </summary>
    /// <param name="name">string: category name.</param>
    /// <param name="level">int: complexity level between 1 and 5.</param>
    /// <param name="description">string?: optional description.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;int&gt;: identifier of the new category.</returns>
    public async Task<int> CreateAsync(string name, int level, string? description, CancellationToken cancellationToken)
    {
        await EnsureLevelIsFreeAsync(level, excludedCategoryId: null, cancellationToken);
        var category = Category.Create(name, level, description);
        categoryRepository.Add(category);
        await categoryRepository.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    /// <summary>
    /// Edits a category. Business rule: only one category per level.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="name">string: category name.</param>
    /// <param name="level">int: complexity level between 1 and 5.</param>
    /// <param name="description">string?: optional description.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    public async Task UpdateAsync(int categoryId, string name, int level, string? description, CancellationToken cancellationToken)
    {
        var category = await GetAsync(categoryId, cancellationToken);
        await EnsureLevelIsFreeAsync(level, categoryId, cancellationToken);
        category.Update(name, level, description);
        await categoryRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a category. Business rule: only categories without questions can be deleted.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when deleted.</returns>
    public async Task DeleteAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await GetAsync(categoryId, cancellationToken);
        var questionCount = await categoryRepository.CountQuestionsAsync(categoryId, cancellationToken);
        Category.EnsureCanBeDeleted(questionCount);
        categoryRepository.Remove(category);
        await categoryRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Fails when another category already uses the level.
    /// </summary>
    /// <param name="level">int: level to check.</param>
    /// <param name="excludedCategoryId">int?: category being edited, ignored in the check.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task: completes when the level is free.</returns>
    private async Task EnsureLevelIsFreeAsync(int level, int? excludedCategoryId, CancellationToken cancellationToken)
    {
        if (await categoryRepository.IsLevelTakenAsync(level, excludedCategoryId, cancellationToken))
            throw new DomainException($"Ya existe una categoría para el nivel {level}.");
    }
}
