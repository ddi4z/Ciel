using QuizCiel.Tests.Support;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Tests.Domain;

/// <summary>
/// Business rules of the game aggregate and its states.
/// </summary>
public class GameTests
{
    private static readonly DateTime WithinTimeLimitUtc = TestData.GameStartUtc.AddSeconds(5);

    /// <summary>
    /// A new game starts in round 1, in progress and with nothing accumulated.
    /// </summary>
    [Fact]
    public void Start_CreatesGameInFirstRoundWithoutPrize()
    {
        var game = TestData.StartedGame();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(1, game.CurrentRoundNumber);
        Assert.Equal(0m, game.AccumulatedPrize);
        Assert.Single(game.Rounds);
    }

    /// <summary>
    /// Answering the five rounds correctly wins the sum of all round prizes.
    /// </summary>
    [Fact]
    public void Answer_AllRoundsCorrect_WinsAccumulatedPrize()
    {
        var game = TestData.StartedGame();

        for (var roundNumber = 1; roundNumber <= GameRules.TotalRounds; roundNumber++)
            TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(TestData.PrizesByRound.Sum(), game.FinalPrize);
    }

    /// <summary>
    /// A correct answer adds the round prize and moves to the next level.
    /// </summary>
    [Fact]
    public void Answer_Correct_AddsPrizeAndLevelsUp()
    {
        var game = TestData.StartedGame();
        var correctOptionId = game.CurrentRound.Question.GetCorrectOption().Id;

        var outcome = game.Answer(correctOptionId, WithinTimeLimitUtc);

        Assert.Equal(AnswerOutcome.CorrectAndLeveledUp, outcome);
        Assert.Equal(2, game.CurrentRoundNumber);
        Assert.Equal(TestData.PrizesByRound[0], game.AccumulatedPrize);
    }

    /// <summary>
    /// A wrong answer ends the game and the player loses everything accumulated.
    /// </summary>
    [Fact]
    public void Answer_WrongInThirdRound_LosesEverything()
    {
        var game = TestData.StartedGame();
        TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);
        TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);

        var outcome = game.Answer(TestData.WrongOptionIdOfCurrentRound(game), WithinTimeLimitUtc);

        Assert.Equal(AnswerOutcome.Wrong, outcome);
        Assert.Equal(GameStatus.Lost, game.Status);
        Assert.Equal(0m, game.FinalPrize);
    }

    /// <summary>
    /// Withdrawing before answering keeps what was accumulated in the previous rounds.
    /// </summary>
    [Fact]
    public void Withdraw_InThirdRound_KeepsPrizeOfPreviousRounds()
    {
        var game = TestData.StartedGame();
        TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);
        TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);

        var finalStatus = game.Withdraw(WithinTimeLimitUtc);

        Assert.Equal(GameStatus.Withdrawn, finalStatus);
        Assert.Equal(TestData.PrizesByRound[0] + TestData.PrizesByRound[1], game.FinalPrize);
    }

    /// <summary>
    /// Answering after the time limit (plus grace period) force-ends the game without prize.
    /// </summary>
    [Fact]
    public void Answer_AfterTimeLimit_ForcesEndWithoutPrize()
    {
        var game = TestData.StartedGame();
        TestData.AnswerCorrectlyAndContinue(game, WithinTimeLimitUtc);
        var lateInstantUtc = WithinTimeLimitUtc.AddSeconds(TestData.TimeLimitSeconds + GameRules.AnswerGracePeriodSeconds + 1);

        var outcome = game.Answer(game.CurrentRound.Question.GetCorrectOption().Id, lateInstantUtc);

        Assert.Equal(AnswerOutcome.TimedOut, outcome);
        Assert.Equal(GameStatus.ForcedEnd, game.Status);
        Assert.Equal(0m, game.FinalPrize);
    }

    /// <summary>
    /// An answer arriving within the grace period after the deadline is still accepted.
    /// </summary>
    [Fact]
    public void Answer_WithinGracePeriod_IsAccepted()
    {
        var game = TestData.StartedGame();
        var instantInsideGracePeriodUtc = TestData.GameStartUtc.AddSeconds(TestData.TimeLimitSeconds + 1);

        var outcome = game.Answer(game.CurrentRound.Question.GetCorrectOption().Id, instantInsideGracePeriodUtc);

        Assert.Equal(AnswerOutcome.CorrectAndLeveledUp, outcome);
    }

    /// <summary>
    /// Withdrawing after the time ran out is a forced end, not a withdrawal.
    /// </summary>
    [Fact]
    public void Withdraw_AfterTimeLimit_ForcesEnd()
    {
        var game = TestData.StartedGame();
        var lateInstantUtc = TestData.GameStartUtc.AddMinutes(5);

        var finalStatus = game.Withdraw(lateInstantUtc);

        Assert.Equal(GameStatus.ForcedEnd, finalStatus);
        Assert.Equal(0m, game.FinalPrize);
    }

    /// <summary>
    /// A finished game rejects new answers and withdrawals.
    /// </summary>
    [Fact]
    public void FinishedGame_RejectsAnyAction()
    {
        var game = TestData.StartedGame();
        game.Answer(TestData.WrongOptionIdOfCurrentRound(game), WithinTimeLimitUtc);

        Assert.Throws<DomainException>(() => game.Answer(1, WithinTimeLimitUtc));
        Assert.Throws<DomainException>(() => game.Withdraw(WithinTimeLimitUtc));
    }

    /// <summary>
    /// An option that does not belong to the current question is rejected.
    /// </summary>
    [Fact]
    public void Answer_WithForeignOption_IsRejected()
    {
        var game = TestData.StartedGame();
        var foreignOptionId = TestData.QuestionForLevel(1).Options[0].Id;

        Assert.Throws<DomainException>(() => game.Answer(foreignOptionId, WithinTimeLimitUtc));
    }

    /// <summary>
    /// A round only accepts questions whose category level matches the round number.
    /// </summary>
    [Fact]
    public void AssignQuestion_FromWrongLevel_IsRejected()
    {
        var game = TestData.StartedGame();
        game.Answer(game.CurrentRound.Question.GetCorrectOption().Id, WithinTimeLimitUtc);

        Assert.Throws<DomainException>(() =>
            game.AssignQuestion(TestData.RoundConfigFor(2), TestData.QuestionForLevel(4), WithinTimeLimitUtc));
    }

    /// <summary>
    /// A round cannot receive a second question.
    /// </summary>
    [Fact]
    public void AssignQuestion_Twice_IsRejected()
    {
        var game = TestData.StartedGame();

        Assert.Throws<DomainException>(() =>
            game.AssignQuestion(TestData.RoundConfigFor(1), TestData.QuestionForLevel(1), WithinTimeLimitUtc));
    }

    /// <summary>
    /// Prize and time limit are copied into the round, so later configuration changes do not affect it.
    /// </summary>
    [Fact]
    public void Round_KeepsPrizeAndTimeLimitSnapshot()
    {
        var game = TestData.StartedGame();

        Assert.Equal(TestData.PrizesByRound[0], game.CurrentRound.Prize);
        Assert.Equal(TestData.GameStartUtc.AddSeconds(TestData.TimeLimitSeconds), game.CurrentRound.DeadlineUtc);
    }
}
