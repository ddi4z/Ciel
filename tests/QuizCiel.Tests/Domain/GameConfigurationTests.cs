using QuizCiel.Tests.Support;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Tests.Domain;

/// <summary>
/// Business rules that apply to the whole set of round settings.
/// </summary>
public class GameConfigurationTests
{
    /// <summary>
    /// Five rounds with increasing prizes and valid times are accepted and ordered.
    /// </summary>
    [Fact]
    public void Create_WithIncreasingPrizes_IsAccepted()
    {
        var shuffledSettings = TestData.ValidRoundSettings().OrderByDescending(round => round.RoundNumber);

        var configuration = GameConfiguration.Create(shuffledSettings);

        Assert.Equal([1, 2, 3, 4, 5], configuration.Rounds.Select(round => round.RoundNumber));
    }

    /// <summary>
    /// A round paying the same as the previous one is rejected.
    /// </summary>
    [Fact]
    public void Create_WithEqualPrizeToPreviousRound_IsRejected()
    {
        var roundSettings = TestData.ValidRoundSettings();
        roundSettings[2] = roundSettings[2] with { Prize = roundSettings[1].Prize };

        Assert.Throws<DomainException>(() => GameConfiguration.Create(roundSettings));
    }

    /// <summary>
    /// A round paying less than the previous one is rejected.
    /// </summary>
    [Fact]
    public void Create_WithLowerPrizeThanPreviousRound_IsRejected()
    {
        var roundSettings = TestData.ValidRoundSettings();
        roundSettings[3] = roundSettings[3] with { Prize = roundSettings[2].Prize - 1 };

        Assert.Throws<DomainException>(() => GameConfiguration.Create(roundSettings));
    }

    /// <summary>
    /// A missing round is rejected.
    /// </summary>
    [Fact]
    public void Create_WithFourRounds_IsRejected()
    {
        var fourRoundSettings = TestData.ValidRoundSettings().Take(4);

        Assert.Throws<DomainException>(() => GameConfiguration.Create(fourRoundSettings));
    }

    /// <summary>
    /// A prize with cents is rejected because prizes are whole pesos.
    /// </summary>
    [Fact]
    public void Create_WithFractionalPrize_IsRejected()
    {
        var roundSettings = TestData.ValidRoundSettings();
        roundSettings[0] = roundSettings[0] with { Prize = 100.5m };

        Assert.Throws<DomainException>(() => GameConfiguration.Create(roundSettings));
    }

    /// <summary>
    /// A time limit outside the allowed range is rejected.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(500)]
    public void Create_WithTimeLimitOutOfRange_IsRejected(int invalidTimeLimitSeconds)
    {
        var roundSettings = TestData.ValidRoundSettings();
        roundSettings[0] = roundSettings[0] with { TimeLimitSeconds = invalidTimeLimitSeconds };

        Assert.Throws<DomainException>(() => GameConfiguration.Create(roundSettings));
    }
}
