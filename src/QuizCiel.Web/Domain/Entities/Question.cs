using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// A multiple choice question with exactly four options and a single valid answer.
/// </summary>
public class Question
{
    public int Id { get; private set; }
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string Text { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public List<AnswerOption> Options { get; private set; } = new();

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private Question()
    {
    }

    /// <summary>
    /// Factory method that creates an active question after validating its text and options.
    /// </summary>
    /// <param name="categoryId">int: identifier of the owning category.</param>
    /// <param name="text">string: question statement.</param>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: exactly four options with one correct.</param>
    /// <returns>Question: the new, not yet persisted question.</returns>
    public static Question Create(int categoryId, string text, IReadOnlyList<AnswerOptionDraft> optionDrafts)
    {
        ValidateText(text);
        ValidateOptions(optionDrafts);

        return new Question
        {
            CategoryId = categoryId,
            Text = text.Trim(),
            IsActive = true,
            Options = BuildOptions(optionDrafts)
        };
    }

    /// <summary>
    /// Edits the statement and replaces the options. Business rule: questions already played are immutable,
    /// so the game history keeps showing exactly what the player saw. Replacing (instead of updating in place)
    /// keeps the "single correct option" database index valid at every step of the save.
    /// </summary>
    /// <param name="text">string: new question statement.</param>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: exactly four options with one correct.</param>
    /// <param name="hasBeenPlayed">bool: whether any game round already used this question.</param>
    public void Edit(string text, IReadOnlyList<AnswerOptionDraft> optionDrafts, bool hasBeenPlayed)
    {
        if (hasBeenPlayed)
            throw new DomainException("La pregunta ya fue jugada y no puede editarse. Desactívala y crea una nueva.");
        ValidateText(text);
        ValidateOptions(optionDrafts);

        Text = text.Trim();
        Options.Clear();
        Options.AddRange(BuildOptions(optionDrafts));
    }

    /// <summary>
    /// Returns the options in the order they are shown to the player.
    /// </summary>
    /// <returns>List&lt;AnswerOption&gt;: options ordered by position.</returns>
    public List<AnswerOption> GetOptionsInDisplayOrder()
    {
        return Options.OrderBy(option => option.Position).ToList();
    }

    /// <summary>
    /// Makes the question available for future games again.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Removes the question from future games. Business rule: a category must keep at least
    /// <see cref="GameRules.MinimumActiveQuestionsPerCategory"/> active questions.
    /// </summary>
    /// <param name="activeQuestionsInCategory">int: active questions currently in the category, including this one.</param>
    public void Deactivate(int activeQuestionsInCategory)
    {
        if (!IsActive)
            return;
        if (activeQuestionsInCategory <= GameRules.MinimumActiveQuestionsPerCategory)
            throw new DomainException(
                $"Cada categoría debe conservar al menos {GameRules.MinimumActiveQuestionsPerCategory} preguntas activas.");
        IsActive = false;
    }

    /// <summary>
    /// Finds one of this question's options.
    /// </summary>
    /// <param name="optionId">int: identifier of the option chosen by the player.</param>
    /// <returns>AnswerOption: the matching option.</returns>
    public AnswerOption GetOption(int optionId)
    {
        return Options.SingleOrDefault(option => option.Id == optionId)
            ?? throw new DomainException("La opción seleccionada no pertenece a la pregunta actual.");
    }

    /// <summary>
    /// Returns the single valid answer of the question.
    /// </summary>
    /// <returns>AnswerOption: the correct option.</returns>
    public AnswerOption GetCorrectOption()
    {
        return Options.Single(option => option.IsCorrect);
    }

    /// <summary>
    /// Creates the option entities from validated drafts, numbering them 1 to 4 in the given order.
    /// </summary>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: validated drafts in display order.</param>
    /// <returns>List&lt;AnswerOption&gt;: new options.</returns>
    private static List<AnswerOption> BuildOptions(IReadOnlyList<AnswerOptionDraft> optionDrafts)
    {
        return optionDrafts.Select((draft, index) => AnswerOption.Create(draft, position: index + 1)).ToList();
    }

    /// <summary>
    /// Validates the question statement.
    /// </summary>
    /// <param name="text">string: candidate statement.</param>
    private static void ValidateText(string text)
    {
        var trimmedText = text?.Trim() ?? string.Empty;
        if (trimmedText.Length == 0 || trimmedText.Length > GameRules.QuestionTextMaxLength)
            throw new DomainException($"El enunciado es obligatorio y admite máximo {GameRules.QuestionTextMaxLength} caracteres.");
    }

    /// <summary>
    /// Business rule: exactly four non-empty, distinct options with exactly one correct answer.
    /// </summary>
    /// <param name="optionDrafts">IReadOnlyList&lt;AnswerOptionDraft&gt;: candidate options.</param>
    private static void ValidateOptions(IReadOnlyList<AnswerOptionDraft> optionDrafts)
    {
        if (optionDrafts is null || optionDrafts.Count != GameRules.OptionsPerQuestion)
            throw new DomainException($"La pregunta debe tener exactamente {GameRules.OptionsPerQuestion} opciones.");

        var trimmedTexts = optionDrafts.Select(draft => draft.Text?.Trim() ?? string.Empty).ToList();
        if (trimmedTexts.Any(optionText => optionText.Length == 0 || optionText.Length > GameRules.OptionTextMaxLength))
            throw new DomainException($"Cada opción es obligatoria y admite máximo {GameRules.OptionTextMaxLength} caracteres.");
        if (trimmedTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != trimmedTexts.Count)
            throw new DomainException("Las opciones no pueden repetirse.");
        if (optionDrafts.Count(draft => draft.IsCorrect) != 1)
            throw new DomainException("La pregunta debe tener exactamente una respuesta correcta.");
    }
}
