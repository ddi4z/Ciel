using System.ComponentModel.DataAnnotations;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Services;

namespace QuizCiel.Web.Models;

/// <summary>
/// Login form shared by players (cédula) and the administrator (username).
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu cédula o usuario.")]
    [Display(Name = "Cédula o usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Player self-registration form.
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu número de cédula.")]
    [StringLength(20, ErrorMessage = "La cédula no es válida.")]
    [Display(Name = "Número de cédula")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu nombre completo.")]
    [StringLength(GameRules.PlayerNameMaxLength, MinimumLength = GameRules.PlayerNameMinLength,
        ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [StringLength(100, MinimumLength = GameRules.PasswordMinLength, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Administration home: whether the game can be played and the category overview.
/// </summary>
/// <param name="Readiness">GameReadinessReport: readiness per level and problems found.</param>
/// <param name="Categories">List&lt;CategorySummary&gt;: categories with question counts.</param>
public record AdminDashboardViewModel(GameReadinessReport Readiness, List<CategorySummary> Categories);

/// <summary>
/// Create/edit category form.
/// </summary>
public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(GameRules.CategoryNameMaxLength, ErrorMessage = "El nombre admite máximo {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Range(GameRules.MinimumLevel, GameRules.MaximumLevel, ErrorMessage = "El nivel debe estar entre {1} y {2}.")]
    [Display(Name = "Nivel de complejidad")]
    public int Level { get; set; } = GameRules.MinimumLevel;

    [StringLength(GameRules.CategoryDescriptionMaxLength, ErrorMessage = "La descripción admite máximo {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    /// <summary>
    /// Fills the form with an existing category.
    /// </summary>
    /// <param name="category">Category: category to edit.</param>
    /// <returns>CategoryFormViewModel: prefilled form.</returns>
    public static CategoryFormViewModel FromCategory(Category category)
    {
        return new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Level = category.Level,
            Description = category.Description
        };
    }
}

/// <summary>
/// Question list of one category.
/// </summary>
/// <param name="CategoryId">int: category identifier.</param>
/// <param name="CategoryName">string: category name.</param>
/// <param name="CategoryLevel">int: category level.</param>
/// <param name="Questions">List&lt;QuestionSummary&gt;: questions of the category.</param>
public record QuestionListViewModel(int CategoryId, string CategoryName, int CategoryLevel, List<QuestionSummary> Questions)
{
    public int ActiveQuestionCount => Questions.Count(question => question.IsActive);
}

/// <summary>
/// Create/edit question form with its four options.
/// </summary>
public class QuestionFormViewModel
{
    public int? Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }

    [Required(ErrorMessage = "El enunciado es obligatorio.")]
    [StringLength(GameRules.QuestionTextMaxLength, ErrorMessage = "El enunciado admite máximo {1} caracteres.")]
    [Display(Name = "Enunciado")]
    public string Text { get; set; } = string.Empty;

    [MinLength(GameRules.OptionsPerQuestion), MaxLength(GameRules.OptionsPerQuestion)]
    public List<string> OptionTexts { get; set; } = Enumerable.Repeat(string.Empty, GameRules.OptionsPerQuestion).ToList();

    [Range(0, GameRules.OptionsPerQuestion - 1, ErrorMessage = "Marca la respuesta correcta.")]
    [Display(Name = "Respuesta correcta")]
    public int CorrectOptionIndex { get; set; }

    /// <summary>
    /// Converts the form into option drafts for the domain.
    /// </summary>
    /// <returns>List&lt;AnswerOptionDraft&gt;: four drafts in display order.</returns>
    public List<AnswerOptionDraft> ToOptionDrafts()
    {
        return OptionTexts
            .Select((optionText, position) => new AnswerOptionDraft(optionText ?? string.Empty, position == CorrectOptionIndex))
            .ToList();
    }

    /// <summary>
    /// Fills the form with an existing question.
    /// </summary>
    /// <param name="question">Question: question with category and options loaded.</param>
    /// <param name="hasBeenPlayed">bool: whether the question is read-only because it was played.</param>
    /// <returns>QuestionFormViewModel: prefilled form.</returns>
    public static QuestionFormViewModel FromQuestion(Question question, bool hasBeenPlayed)
    {
        var optionsInDisplayOrder = question.GetOptionsInDisplayOrder();
        return new QuestionFormViewModel
        {
            Id = question.Id,
            CategoryId = question.CategoryId,
            CategoryName = question.Category.Name,
            IsReadOnly = hasBeenPlayed,
            Text = question.Text,
            OptionTexts = optionsInDisplayOrder.Select(option => option.Text).ToList(),
            CorrectOptionIndex = optionsInDisplayOrder.FindIndex(option => option.IsCorrect)
        };
    }
}

/// <summary>
/// One row of the round settings form.
/// </summary>
public class RoundSettingsInput
{
    public int RoundNumber { get; set; }

    [Range(1, 999_999_999_999, ErrorMessage = "El premio debe ser mayor que cero.")]
    [Display(Name = "Premio (COP)")]
    public decimal Prize { get; set; }

    [Range(GameRules.MinimumTimeLimitSeconds, GameRules.MaximumTimeLimitSeconds, ErrorMessage = "El tiempo debe estar entre {1} y {2} segundos.")]
    [Display(Name = "Tiempo (segundos)")]
    public int TimeLimitSeconds { get; set; }
}

/// <summary>
/// Form that edits the five rounds together, because their rules depend on each other.
/// </summary>
public class RoundSettingsFormViewModel
{
    public List<RoundSettingsInput> Rounds { get; set; } = new();

    /// <summary>
    /// Builds the form with a row per round, leaving empty values for rounds never configured.
    /// </summary>
    /// <param name="storedSettings">IReadOnlyList&lt;RoundSettings&gt;: settings currently stored.</param>
    /// <returns>RoundSettingsFormViewModel: form with five rows.</returns>
    public static RoundSettingsFormViewModel FromSettings(IReadOnlyList<RoundSettings> storedSettings)
    {
        var storedByRound = storedSettings.ToDictionary(settings => settings.RoundNumber);
        return new RoundSettingsFormViewModel
        {
            Rounds = Enumerable.Range(1, GameRules.TotalRounds)
                .Select(roundNumber => new RoundSettingsInput
                {
                    RoundNumber = roundNumber,
                    Prize = storedByRound.GetValueOrDefault(roundNumber)?.Prize ?? 0,
                    TimeLimitSeconds = storedByRound.GetValueOrDefault(roundNumber)?.TimeLimitSeconds ?? GameRules.MinimumTimeLimitSeconds
                })
                .ToList()
        };
    }

    /// <summary>
    /// Converts the form rows into domain settings.
    /// </summary>
    /// <returns>List&lt;RoundSettings&gt;: settings of every row.</returns>
    public List<RoundSettings> ToSettings()
    {
        return Rounds.Select(row => new RoundSettings(row.RoundNumber, row.Prize, row.TimeLimitSeconds)).ToList();
    }
}
