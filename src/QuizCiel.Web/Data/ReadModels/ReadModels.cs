using QuizCiel.Web.Domain.Enums;

namespace QuizCiel.Web.Data.ReadModels;

/// <summary>
/// Category row for the administration list, projected directly by the ORM.
/// </summary>
/// <param name="Id">int: category identifier.</param>
/// <param name="Name">string: category name.</param>
/// <param name="Level">int: complexity level.</param>
/// <param name="Description">string?: optional description.</param>
/// <param name="ActiveQuestionCount">int: active questions.</param>
/// <param name="TotalQuestionCount">int: all questions.</param>
public record CategorySummary(int Id, string Name, int Level, string? Description, int ActiveQuestionCount, int TotalQuestionCount);

/// <summary>
/// Question row for the administration list, projected directly by the ORM.
/// </summary>
/// <param name="Id">int: question identifier.</param>
/// <param name="Text">string: question statement.</param>
/// <param name="IsActive">bool: whether it can appear in new games.</param>
/// <param name="CorrectOptionText">string: text of the valid answer.</param>
/// <param name="TimesPlayed">int: how many game rounds used it.</param>
public record QuestionSummary(int Id, string Text, bool IsActive, string CorrectOptionText, int TimesPlayed);

/// <summary>
/// Game row for the administrator's game and prize lists, projected directly by the ORM.
/// </summary>
/// <param name="GameId">Guid: game identifier.</param>
/// <param name="PlayerFullName">string: player full name.</param>
/// <param name="PlayerDocumentNumber">string: player cédula, used to pay the right person.</param>
/// <param name="Status">GameStatus: current or final status.</param>
/// <param name="RoundReached">int: last round reached.</param>
/// <param name="AccumulatedPrize">decimal: prize accumulated so far.</param>
/// <param name="FinalPrize">decimal?: prize taken home, null while in progress.</param>
/// <param name="StartedAt">DateTime: start instant in UTC.</param>
/// <param name="EndedAt">DateTime?: end instant in UTC.</param>
public record GameHistoryItem(
    Guid GameId,
    string PlayerFullName,
    string PlayerDocumentNumber,
    GameStatus Status,
    int RoundReached,
    decimal AccumulatedPrize,
    decimal? FinalPrize,
    DateTime StartedAt,
    DateTime? EndedAt);
