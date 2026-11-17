using System.Text.RegularExpressions;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Domain.Entities;

/// <summary>
/// A registered participant, identified by a unique national document number (cédula) so prizes are paid
/// to the right person even when two players share the same name.
/// </summary>
public partial class Player
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor used only by EF Core to materialize rows; code must use <see cref="Create"/>.
    /// </summary>
    private Player()
    {
    }

    /// <summary>
    /// Factory method that creates a player linked to its login account, with validated data.
    /// </summary>
    /// <param name="userId">string: identifier of the login account that owns this player.</param>
    /// <param name="documentNumber">string: cédula as typed (dots and spaces are ignored).</param>
    /// <param name="fullName">string: full name as typed.</param>
    /// <param name="createdAtUtc">DateTime: creation instant in UTC.</param>
    /// <returns>Player: the new, not yet persisted player.</returns>
    public static Player Create(string userId, string documentNumber, string fullName, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("El jugador debe estar asociado a una cuenta.");

        return new Player
        {
            UserId = userId,
            DocumentNumber = NormalizeDocumentNumber(documentNumber),
            FullName = NormalizeFullName(fullName),
            CreatedAt = createdAtUtc
        };
    }

    /// <summary>
    /// Business rule: each person takes part only once, whatever the outcome of that game.
    /// The unique index on Game(PlayerId) backs it up in the database.
    /// </summary>
    /// <param name="hasAlreadyPlayed">bool: whether this player already started a game.</param>
    public static void EnsureCanStartGame(bool hasAlreadyPlayed)
    {
        if (hasAlreadyPlayed)
            throw new DomainException("Ya participaste en el concurso. Cada persona puede jugar una sola vez.");
    }

    /// <summary>
    /// Removes dots, dashes and spaces, then validates that the cédula has only digits and a valid length.
    /// </summary>
    /// <param name="documentNumber">string: cédula as typed.</param>
    /// <returns>string: digits only.</returns>
    public static string NormalizeDocumentNumber(string documentNumber)
    {
        var digitsOnly = StripDocumentSeparators(documentNumber);

        if (!DigitsPattern().IsMatch(digitsOnly)
            || digitsOnly.Length < GameRules.DocumentNumberMinLength
            || digitsOnly.Length > GameRules.DocumentNumberMaxLength)
            throw new DomainException(
                $"La cédula debe tener entre {GameRules.DocumentNumberMinLength} y {GameRules.DocumentNumberMaxLength} dígitos.");

        return digitsOnly;
    }

    /// <summary>
    /// Removes the separators people commonly type inside a cédula (dots, dashes, spaces) without validating it.
    /// </summary>
    /// <param name="documentNumber">string: cédula as typed.</param>
    /// <returns>string: the same text without separators.</returns>
    public static string StripDocumentSeparators(string documentNumber)
    {
        return DocumentSeparatorsPattern().Replace(documentNumber ?? string.Empty, string.Empty);
    }

    /// <summary>
    /// Trims and collapses repeated spaces, then validates length and allowed characters.
    /// </summary>
    /// <param name="fullName">string: full name as typed.</param>
    /// <returns>string: the normalized name.</returns>
    public static string NormalizeFullName(string fullName)
    {
        var normalizedName = RepeatedSpacesPattern().Replace(fullName?.Trim() ?? string.Empty, " ");

        if (normalizedName.Length < GameRules.PlayerNameMinLength || normalizedName.Length > GameRules.PlayerNameMaxLength)
            throw new DomainException(
                $"El nombre debe tener entre {GameRules.PlayerNameMinLength} y {GameRules.PlayerNameMaxLength} caracteres.");
        if (!AllowedNamePattern().IsMatch(normalizedName))
            throw new DomainException("El nombre solo puede contener letras, espacios, apóstrofos y guiones.");

        return normalizedName;
    }

    /// <summary>
    /// Matches one or more consecutive whitespace characters.
    /// </summary>
    /// <returns>Regex: compiled pattern.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedSpacesPattern();

    /// <summary>
    /// Matches names made only of letters (including accents), apostrophes, hyphens and spaces.
    /// </summary>
    /// <returns>Regex: compiled pattern.</returns>
    [GeneratedRegex(@"^[\p{L}][\p{L}'\- ]*$")]
    private static partial Regex AllowedNamePattern();

    /// <summary>
    /// Matches the separators people commonly type inside a cédula (dots, dashes, spaces).
    /// </summary>
    /// <returns>Regex: compiled pattern.</returns>
    [GeneratedRegex(@"[\s.\-]")]
    private static partial Regex DocumentSeparatorsPattern();

    /// <summary>
    /// Matches strings made only of digits.
    /// </summary>
    /// <returns>Regex: compiled pattern.</returns>
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsPattern();
}
