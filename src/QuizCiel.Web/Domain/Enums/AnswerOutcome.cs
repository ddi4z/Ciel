namespace QuizCiel.Web.Domain.Enums;

/// <summary>
/// Result of answering the current question of a game.
/// </summary>
public enum AnswerOutcome
{
    CorrectAndLeveledUp = 0,
    CorrectAndWonGame = 1,
    Wrong = 2,
    TimedOut = 3
}
