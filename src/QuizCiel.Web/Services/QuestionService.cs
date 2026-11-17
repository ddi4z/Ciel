using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Services;

/// <summary>
/// Service layer for the administrator's question use cases.
/// </summary>
public class QuestionService
{
    private readonly IQuestionRepository questionRepository;
    private readonly ICategoryRepository categoryRepository;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="questionRepository">IQuestionRepository: question data access.</param>
    /// <param name="categoryRepository">ICategoryRepository: category data access.</param>
    public QuestionService(IQuestionRepository questionRepository, ICategoryRepository categoryRepository)
    {
        this.questionRepository = questionRepository;
        this.categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Lists the questions of a category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;QuestionSummary&gt;&gt;: question rows.</returns>
    public Task<List<QuestionSummary>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return questionRepository.GetSummariesByCategoryAsync(categoryId, cancellationToken);
    }

    /// <summary>
    /// Loads a question with its options or fails with a not found error.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Question&gt;: the question.</returns>
    public async Task<Question> GetAsync(int questionId, CancellationToken cancellationToken)
    {
        return await questionRepository.FindWithOptionsAsync(questionId, cancellationToken)
            ?? throw new NotFoundException("La pregunta no existe.");
    }

    /// <summary>
    /// Loads a question only if it belongs to the given category, so a URL that mixes a category with a
    /// question of another category is treated as "not found".
    /// </summary>
    /// <param name="categoryId">int: category identifier taken from the URL.</param>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Question&gt;: the question.</returns>
    public async Task<Question> GetInCategoryAsync(int categoryId, int questionId, CancellationToken cancellationToken)
    {
        var question = await GetAsync(questionId, cancellationToken);
        if (question.CategoryId != categoryId)
            throw new NotFoundException("La pregunta no pertenece a esta categoría.");
        return question;
    }

    /// <summary>
    /// Checks whether a question was already used in a game, which makes it read-only.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;bool&gt;: true when it was played.</returns>
    public Task<bool> HasBeenPlayedAsync(int questionId, CancellationToken cancellationToken)
    {
        return questionRepository.HasBeenPlayedAsync(questionId, cancellationToken);
    }

    /// <summary>
    /// Creates an active question in an existing category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="text">string: question statement.</param>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: four options with one correct.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;int&gt;: identifier of the new question.</returns>
    public async Task<int> CreateAsync(int categoryId, string text, IReadOnlyList<AnswerOptionDraft> optionDrafts, CancellationToken cancellationToken)
    {
        _ = await categoryRepository.FindAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("La categoría no existe.");

        var question = Question.Create(categoryId, text, optionDrafts);
        questionRepository.Add(question);
        await questionRepository.SaveChangesAsync(cancellationToken);
        return question.Id;
    }

    /// <summary>
    /// Edits a question. Business rule enforced by the domain: played questions are immutable.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="text">string: question statement.</param>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: four options with one correct.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    public async Task UpdateAsync(int questionId, string text, IReadOnlyList<AnswerOptionDraft> optionDrafts, CancellationToken cancellationToken)
    {
        var question = await GetAsync(questionId, cancellationToken);
        var hasBeenPlayed = await questionRepository.HasBeenPlayedAsync(questionId, cancellationToken);
        question.Edit(text, optionDrafts, hasBeenPlayed);
        await questionRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Makes a question available for new games.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;int&gt;: identifier of the question's category.</returns>
    public async Task<int> ActivateAsync(int questionId, CancellationToken cancellationToken)
    {
        var question = await GetAsync(questionId, cancellationToken);
        question.Activate();
        await questionRepository.SaveChangesAsync(cancellationToken);
        return question.CategoryId;
    }

    /// <summary>
    /// Removes a question from new games. Business rule enforced by the domain: the category keeps the minimum of active questions.
    /// </summary>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;int&gt;: identifier of the question's category.</returns>
    public async Task<int> DeactivateAsync(int questionId, CancellationToken cancellationToken)
    {
        var question = await GetAsync(questionId, cancellationToken);
        var activeQuestionsInCategory = await questionRepository.CountActiveInCategoryAsync(question.CategoryId, cancellationToken);
        question.Deactivate(activeQuestionsInCategory);
        await questionRepository.SaveChangesAsync(cancellationToken);
        return question.CategoryId;
    }
}
