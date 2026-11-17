using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Services.Strategies;

/// <summary>
/// Picks a uniformly random active question of the requested level. Questions never repeat inside a game
/// because every round uses a different level.
/// </summary>
public class RandomQuestionSelector : IQuestionSelector
{
    private readonly IQuestionRepository questionRepository;
    private readonly Random randomNumberGenerator;

    /// <summary>
    /// Creates the selector.
    /// </summary>
    /// <param name="questionRepository">IQuestionRepository: source of candidate questions.</param>
    /// <param name="randomNumberGenerator">Random: generator, injectable to make tests deterministic.</param>
    public RandomQuestionSelector(IQuestionRepository questionRepository, Random randomNumberGenerator)
    {
        this.questionRepository = questionRepository;
        this.randomNumberGenerator = randomNumberGenerator;
    }

    /// <inheritdoc />
    public async Task<Question> SelectForLevelAsync(int level, CancellationToken cancellationToken)
    {
        var candidateQuestionIds = await questionRepository.GetActiveQuestionIdsByLevelAsync(level, cancellationToken);
        if (candidateQuestionIds.Count == 0)
            throw new DomainException($"No hay preguntas activas para el nivel {level}.");

        var chosenQuestionId = candidateQuestionIds[randomNumberGenerator.Next(candidateQuestionIds.Count)];
        return await questionRepository.FindWithOptionsAsync(chosenQuestionId, cancellationToken)
            ?? throw new NotFoundException("La pregunta seleccionada ya no existe.");
    }
}
