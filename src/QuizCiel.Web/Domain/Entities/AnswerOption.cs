namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// One of the four possible answers of a question, shown in a fixed position (1 to 4).
/// </summary>
public class AnswerOption
{
    public int Id { get; private set; }
    public int QuestionId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private AnswerOption()
    {
    }

    /// <summary>
    /// Creates an option from an already validated draft. Only <see cref="Question"/> may call it.
    /// </summary>
    /// <param name="draft">AnswerOptionDraft: validated text and correctness flag.</param>
    /// <param name="position">int: display position between 1 and 4.</param>
    /// <returns>AnswerOption: the new option.</returns>
    internal static AnswerOption Create(AnswerOptionDraft draft, int position)
    {
        return new AnswerOption { Position = position, Text = draft.Text.Trim(), IsCorrect = draft.IsCorrect };
    }
}

/// <summary>
/// Input data used to create or edit an answer option.
/// </summary>
/// <param name="Text">string: option text shown to the player.</param>
/// <param name="IsCorrect">bool: whether this option is the valid answer.</param>
public record AnswerOptionDraft(string Text, bool IsCorrect);
