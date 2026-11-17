using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Administrator use case "Manage questions and options".
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("admin/categories/{categoryId:int}/questions")]
public class AdminQuestionsController : AppController
{
    private readonly QuestionService questionService;
    private readonly CategoryService categoryService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="questionService">QuestionService: question use cases.</param>
    /// <param name="categoryService">CategoryService: category lookups.</param>
    public AdminQuestionsController(QuestionService questionService, CategoryService categoryService)
    {
        this.questionService = questionService;
        this.categoryService = categoryService;
    }

    /// <summary>
    /// Lists the questions of a category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the list view.</returns>
    [HttpGet("")]
    public async Task<IActionResult> Index(int categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetAsync(categoryId, cancellationToken);
        var questions = await questionService.GetByCategoryAsync(categoryId, cancellationToken);
        return View(new QuestionListViewModel(category.Id, category.Name, category.Level, questions));
    }

    /// <summary>
    /// Shows the empty question form.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the form view.</returns>
    [HttpGet("create")]
    public async Task<IActionResult> Create(int categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetAsync(categoryId, cancellationToken);
        return View("Form", new QuestionFormViewModel { CategoryId = category.Id, CategoryName = category.Name });
    }

    /// <summary>
    /// Shows the form of an existing question (read-only if it was already played).
    /// </summary>
    /// <param name="categoryId">int: category identifier from the route.</param>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the form view.</returns>
    [HttpGet("{questionId:int}/edit")]
    public async Task<IActionResult> Edit(int categoryId, int questionId, CancellationToken cancellationToken)
    {
        var question = await questionService.GetInCategoryAsync(categoryId, questionId, cancellationToken);
        var hasBeenPlayed = await questionService.HasBeenPlayedAsync(questionId, cancellationToken);
        return View("Form", QuestionFormViewModel.FromQuestion(question, hasBeenPlayed));
    }

    /// <summary>
    /// Creates or updates a question depending on whether the form has an identifier.
    /// </summary>
    /// <param name="categoryId">int: category identifier from the route.</param>
    /// <param name="questionForm">QuestionFormViewModel: posted form.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the list, or the form with errors.</returns>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int categoryId, QuestionFormViewModel questionForm, CancellationToken cancellationToken)
    {
        questionForm.CategoryId = categoryId;
        if (questionForm.Id is int editedQuestionId)
            await questionService.GetInCategoryAsync(categoryId, editedQuestionId, cancellationToken);
        if (!ModelState.IsValid)
            return View("Form", questionForm);

        try
        {
            if (questionForm.Id is int existingQuestionId)
                await questionService.UpdateAsync(existingQuestionId, questionForm.Text, questionForm.ToOptionDrafts(), cancellationToken);
            else
                await questionService.CreateAsync(categoryId, questionForm.Text, questionForm.ToOptionDrafts(), cancellationToken);
        }
        catch (DomainException ruleViolation)
        {
            AddRuleViolationToForm(ruleViolation);
            return View("Form", questionForm);
        }

        ShowSuccess("Pregunta guardada.");
        return RedirectToAction(nameof(Index), new { categoryId });
    }

    /// <summary>
    /// Activates or deactivates a question.
    /// </summary>
    /// <param name="categoryId">int: category identifier from the route.</param>
    /// <param name="questionId">int: question identifier.</param>
    /// <param name="activate">bool: true to activate, false to deactivate.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the list.</returns>
    [HttpPost("{questionId:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int categoryId, int questionId, bool activate, CancellationToken cancellationToken)
    {
        await questionService.GetInCategoryAsync(categoryId, questionId, cancellationToken);
        try
        {
            if (activate)
                await questionService.ActivateAsync(questionId, cancellationToken);
            else
                await questionService.DeactivateAsync(questionId, cancellationToken);
            ShowSuccess(activate ? "Pregunta activada." : "Pregunta desactivada.");
        }
        catch (DomainException ruleViolation)
        {
            ShowRuleViolation(ruleViolation);
        }
        return RedirectToAction(nameof(Index), new { categoryId });
    }
}
