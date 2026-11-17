using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Administrator use case "Manage categories".
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
[Route("admin/categories")]
public class AdminCategoriesController : AppController
{
    private readonly CategoryService categoryService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="categoryService">CategoryService: category use cases.</param>
    public AdminCategoriesController(CategoryService categoryService)
    {
        this.categoryService = categoryService;
    }

    /// <summary>
    /// Lists every category.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the list view.</returns>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await categoryService.GetSummariesAsync(cancellationToken));
    }

    /// <summary>
    /// Shows the empty category form.
    /// </summary>
    /// <returns>IActionResult: the form view.</returns>
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View("Form", new CategoryFormViewModel());
    }

    /// <summary>
    /// Shows the form of an existing category.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: the form view.</returns>
    [HttpGet("{categoryId:int}/edit")]
    public async Task<IActionResult> Edit(int categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetAsync(categoryId, cancellationToken);
        return View("Form", CategoryFormViewModel.FromCategory(category));
    }

    /// <summary>
    /// Creates or updates a category depending on whether the form has an identifier.
    /// </summary>
    /// <param name="categoryForm">CategoryFormViewModel: posted form.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the list, or the form with errors.</returns>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CategoryFormViewModel categoryForm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Form", categoryForm);

        try
        {
            if (categoryForm.Id is int existingCategoryId)
                await categoryService.UpdateAsync(existingCategoryId, categoryForm.Name, categoryForm.Level, categoryForm.Description, cancellationToken);
            else
                await categoryService.CreateAsync(categoryForm.Name, categoryForm.Level, categoryForm.Description, cancellationToken);
        }
        catch (DomainException ruleViolation)
        {
            AddRuleViolationToForm(ruleViolation);
            return View("Form", categoryForm);
        }

        ShowSuccess("Categoría guardada.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Deletes a category without questions.
    /// </summary>
    /// <param name="categoryId">int: category identifier.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the list.</returns>
    [HttpPost("{categoryId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            await categoryService.DeleteAsync(categoryId, cancellationToken);
            ShowSuccess("Categoría eliminada.");
        }
        catch (DomainException ruleViolation)
        {
            ShowRuleViolation(ruleViolation);
        }
        return RedirectToAction(nameof(Index));
    }
}
