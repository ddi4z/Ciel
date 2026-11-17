using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Services.Strategies;

/// <summary>
/// Strategy pattern: decides which question is asked in a round.
/// </summary>
public interface IQuestionSelector
{
    /// <summary>
    /// Picks one active question of the given level.
    /// </summary>
    /// <param name="level">int: category level, equal to the round number.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Question&gt;: the chosen question with category and options loaded.</returns>
    Task<Question> SelectForLevelAsync(int level, CancellationToken cancellationToken);
}
