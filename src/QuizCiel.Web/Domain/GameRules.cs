namespace QuizCiel.Web.Domain;

/// <summary>
/// Central catalog of the business constants that govern the quiz game.
/// </summary>
public static class GameRules
{
    public const int TotalRounds = 5;
    public const int OptionsPerQuestion = 4;
    public const int MinimumActiveQuestionsPerCategory = 5;
    public const int MinimumLevel = 1;
    public const int MaximumLevel = TotalRounds;

    public const int MinimumTimeLimitSeconds = 10;
    public const int MaximumTimeLimitSeconds = 120;
    public const int AnswerGracePeriodSeconds = 2;

    public const int PlayerNameMinLength = 3;
    public const int PlayerNameMaxLength = 80;
    public const int DocumentNumberMinLength = 6;
    public const int DocumentNumberMaxLength = 10;
    public const int PasswordMinLength = 8;
    public const int CategoryNameMaxLength = 80;
    public const int CategoryDescriptionMaxLength = 250;
    public const int QuestionTextMaxLength = 300;
    public const int OptionTextMaxLength = 150;
}
