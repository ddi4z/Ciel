namespace QuizCiel.Web.Domain.Enums;

/// <summary>
/// Lifecycle states of a game. Every value except <see cref="InProgress"/> is final.
/// </summary>
public enum GameStatus
{
    InProgress = 0,
    Won = 1,
    Withdrawn = 2,
    Lost = 3,
    ForcedEnd = 4
}
