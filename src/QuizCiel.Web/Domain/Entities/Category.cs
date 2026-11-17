using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// Groups questions by complexity level. Each level (1 to 5) maps to the game round with the same number.
/// </summary>
public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public string? Description { get; private set; }
    public List<Question> Questions { get; private set; } = new();

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private Category()
    {
    }

    /// <summary>
    /// Factory method that creates a valid category.
    /// </summary>
    /// <param name="name">string: display name, required.</param>
    /// <param name="level">int: complexity level between 1 and 5.</param>
    /// <param name="description">string?: optional description.</param>
    /// <returns>Category: the new, not yet persisted category.</returns>
    public static Category Create(string name, int level, string? description)
    {
        var category = new Category();
        category.Update(name, level, description);
        return category;
    }

    /// <summary>
    /// Changes the category data after validating it.
    /// </summary>
    /// <param name="name">string: display name, required.</param>
    /// <param name="level">int: complexity level between 1 and 5.</param>
    /// <param name="description">string?: optional description.</param>
    public void Update(string name, int level, string? description)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (trimmedName.Length == 0 || trimmedName.Length > GameRules.CategoryNameMaxLength)
            throw new DomainException($"El nombre de la categoría es obligatorio y admite máximo {GameRules.CategoryNameMaxLength} caracteres.");
        if (level < GameRules.MinimumLevel || level > GameRules.MaximumLevel)
            throw new DomainException($"El nivel debe estar entre {GameRules.MinimumLevel} y {GameRules.MaximumLevel}.");
        if (trimmedDescription?.Length > GameRules.CategoryDescriptionMaxLength)
            throw new DomainException($"La descripción admite máximo {GameRules.CategoryDescriptionMaxLength} caracteres.");

        Name = trimmedName;
        Level = level;
        Description = trimmedDescription;
    }

    /// <summary>
    /// Business rule: a category can only be deleted while it has no questions.
    /// </summary>
    /// <param name="questionCount">int: number of questions (active or not) in the category.</param>
    public static void EnsureCanBeDeleted(int questionCount)
    {
        if (questionCount > 0)
            throw new DomainException("No se puede eliminar una categoría que tiene preguntas.");
    }
}
