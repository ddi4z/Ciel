using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Services;

/// <summary>
/// Service layer for the "Configure rounds" use case (prize and time limit per round).
/// </summary>
public class RoundSettingsService
{
    private readonly IRoundConfigRepository roundConfigRepository;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="roundConfigRepository">IRoundConfigRepository: round settings data access.</param>
    public RoundSettingsService(IRoundConfigRepository roundConfigRepository)
    {
        this.roundConfigRepository = roundConfigRepository;
    }

    /// <summary>
    /// Returns the stored settings of every round.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;RoundSettings&gt;&gt;: settings ordered by round; may be incomplete if never configured.</returns>
    public async Task<List<RoundSettings>> GetAsync(CancellationToken cancellationToken)
    {
        var storedRounds = await roundConfigRepository.GetAllAsync(cancellationToken);
        return storedRounds.Select(round => round.ToSettings()).ToList();
    }

    /// <summary>
    /// Validates the five rounds as a whole (see <see cref="GameConfiguration"/>) and saves them together.
    /// Games already in progress keep the prize and time copied into their rounds.
    /// </summary>
    /// <param name="roundSettings">IEnumerable&lt;RoundSettings&gt;: settings of all rounds.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when saved.</returns>
    public async Task SaveAsync(IEnumerable<RoundSettings> roundSettings, CancellationToken cancellationToken)
    {
        var validatedConfiguration = GameConfiguration.Create(roundSettings);
        var storedRoundsByNumber = (await roundConfigRepository.GetAllAsync(cancellationToken))
            .ToDictionary(round => round.RoundNumber);

        foreach (var validatedRound in validatedConfiguration.Rounds)
        {
            if (storedRoundsByNumber.TryGetValue(validatedRound.RoundNumber, out var storedRound))
                storedRound.Apply(validatedRound);
            else
                roundConfigRepository.Add(RoundConfig.Create(validatedRound));
        }

        await roundConfigRepository.SaveChangesAsync(cancellationToken);
    }
}
