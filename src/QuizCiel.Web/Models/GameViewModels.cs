using System.ComponentModel.DataAnnotations;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Services;

namespace QuizCiel.Web.Models;

/// <summary>
/// One step of the prize ladder shown next to the question.
/// </summary>
/// <param name="RoundNumber">int: round number.</param>
/// <param name="Prize">decimal: prize of the round.</param>
/// <param name="IsCurrent">bool: whether it is the round being played.</param>
/// <param name="IsCompleted">bool: whether the round was already won.</param>
public record PrizeStepViewModel(int RoundNumber, decimal Prize, bool IsCurrent, bool IsCompleted)
{
    /// <summary>
    /// Builds the prize ladder, marking the round being played and the rounds already won.
    /// </summary>
    /// <param name="ladderRounds">IEnumerable&lt;RoundConfig&gt;: configured rounds ordered by number.</param>
    /// <param name="currentRoundNumber">int?: round being played, or null outside a game (nothing is marked).</param>
    /// <returns>List&lt;PrizeStepViewModel&gt;: one step per round.</returns>
    public static List<PrizeStepViewModel> BuildLadder(IEnumerable<RoundConfig> ladderRounds, int? currentRoundNumber)
    {
        return ladderRounds
            .Select(round => new PrizeStepViewModel(
                round.RoundNumber,
                round.Prize,
                IsCurrent: round.RoundNumber == currentRoundNumber,
                IsCompleted: round.RoundNumber < currentRoundNumber))
            .ToList();
    }
}

/// <summary>
/// An option as shown to the player. It deliberately has no "is correct" information.
/// </summary>
/// <param name="Id">int: option identifier posted back when answering.</param>
/// <param name="Text">string: option text.</param>
public record AnswerChoiceViewModel(int Id, string Text);

/// <summary>
/// What the signed-in player sees about their own participation on the home page.
/// </summary>
/// <param name="FullName">string: player full name.</param>
/// <param name="MaskedDocumentNumber">string: cédula with all but the last digits hidden.</param>
/// <param name="GameId">Guid?: their game, or null if they have not played.</param>
/// <param name="Status">GameStatus?: status of their game.</param>
/// <param name="PrizeOwed">decimal: prize the player is entitled to (zero while playing or when lost).</param>
public record ParticipationViewModel(string FullName, string MaskedDocumentNumber, Guid? GameId, GameStatus? Status, decimal PrizeOwed)
{
    public bool HasPlayed => GameId.HasValue;
    public bool IsPlaying => Status == GameStatus.InProgress;

    /// <summary>
    /// Builds the summary from the player's participation.
    /// </summary>
    /// <param name="participation">PlayerParticipation: player and optional game.</param>
    /// <returns>ParticipationViewModel: summary for the home page.</returns>
    public static ParticipationViewModel From(PlayerParticipation participation)
    {
        var game = participation.Game;
        return new ParticipationViewModel(
            participation.Player.FullName,
            DisplayFormat.MaskedDocument(participation.Player.DocumentNumber),
            game?.Id,
            game?.Status,
            game?.FinalPrize ?? 0);
    }
}

/// <summary>
/// Home page: rules, prize ladder and, for a signed-in player, their participation.
/// </summary>
public class HomeViewModel
{
    public bool IsGameReady { get; set; }
    public List<PrizeStepViewModel> PrizeLadder { get; set; } = new();
    public ParticipationViewModel? Participation { get; set; }
}

/// <summary>
/// Screen of a game in progress.
/// </summary>
public class GamePlayViewModel
{
    public Guid GameId { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public int RoundNumber { get; init; }
    public int TotalRounds { get; init; } = GameRules.TotalRounds;
    public decimal PrizeAtStake { get; init; }
    public decimal AccumulatedPrize { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public List<AnswerChoiceViewModel> Choices { get; init; } = new();
    public int SecondsRemaining { get; init; }
    public List<PrizeStepViewModel> PrizeLadder { get; init; } = new();

    /// <summary>
    /// Builds the screen from a game in progress, hiding which option is correct.
    /// </summary>
    /// <param name="game">Game: game in progress with its current round loaded.</param>
    /// <param name="ladderRounds">IReadOnlyList&lt;RoundConfig&gt;: configured rounds for the prize ladder.</param>
    /// <param name="nowUtc">DateTime: current instant in UTC.</param>
    /// <returns>GamePlayViewModel: the screen data.</returns>
    public static GamePlayViewModel From(Game game, IReadOnlyList<RoundConfig> ladderRounds, DateTime nowUtc)
    {
        var currentRound = game.CurrentRound;
        var secondsUntilDeadline = (int)Math.Ceiling((currentRound.DeadlineUtc - nowUtc).TotalSeconds);

        return new GamePlayViewModel
        {
            GameId = game.Id,
            PlayerName = game.Player.FullName,
            RoundNumber = currentRound.RoundNumber,
            PrizeAtStake = currentRound.Prize,
            AccumulatedPrize = game.AccumulatedPrize,
            QuestionText = currentRound.Question.Text,
            Choices = currentRound.Question.GetOptionsInDisplayOrder()
                .Select(option => new AnswerChoiceViewModel(option.Id, option.Text))
                .ToList(),
            SecondsRemaining = Math.Max(0, secondsUntilDeadline),
            PrizeLadder = PrizeStepViewModel.BuildLadder(ladderRounds, game.CurrentRoundNumber)
        };
    }
}

/// <summary>
/// One played round in the result screen. Correct answers are revealed only once the game is over.
/// </summary>
/// <param name="RoundNumber">int: round number.</param>
/// <param name="QuestionText">string: question asked.</param>
/// <param name="SelectedOptionText">string?: answer given, null if not answered.</param>
/// <param name="CorrectOptionText">string: valid answer.</param>
/// <param name="IsCorrect">bool?: whether the answer was right, null if not answered.</param>
/// <param name="Prize">decimal: prize of the round.</param>
public record PlayedRoundViewModel(int RoundNumber, string QuestionText, string? SelectedOptionText, string CorrectOptionText, bool? IsCorrect, decimal Prize);

/// <summary>
/// Result screen of a finished game.
/// </summary>
public class GameResultViewModel
{
    public Guid GameId { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public GameStatus Status { get; init; }
    public decimal FinalPrize { get; init; }
    public int RoundReached { get; init; }
    public List<PlayedRoundViewModel> Rounds { get; init; } = new();

    /// <summary>
    /// Builds the result screen from a finished game.
    /// </summary>
    /// <param name="game">Game: finished game with rounds, questions and options loaded.</param>
    /// <returns>GameResultViewModel: the screen data.</returns>
    public static GameResultViewModel From(Game game)
    {
        return new GameResultViewModel
        {
            GameId = game.Id,
            PlayerName = game.Player.FullName,
            Status = game.Status,
            FinalPrize = game.FinalPrize ?? game.AccumulatedPrize,
            RoundReached = game.CurrentRoundNumber,
            Rounds = game.Rounds.OrderBy(round => round.RoundNumber).Select(ToPlayedRound).ToList()
        };
    }

    /// <summary>
    /// Converts one game round into its result row.
    /// </summary>
    /// <param name="round">GameRound: round with question and options loaded.</param>
    /// <returns>PlayedRoundViewModel: result row.</returns>
    private static PlayedRoundViewModel ToPlayedRound(GameRound round)
    {
        var selectedOptionText = round.SelectedOptionId.HasValue
            ? round.Question.GetOption(round.SelectedOptionId.Value).Text
            : null;
        return new PlayedRoundViewModel(
            round.RoundNumber,
            round.Question.Text,
            selectedOptionText,
            round.Question.GetCorrectOption().Text,
            round.IsCorrect,
            round.Prize);
    }
}

/// <summary>
/// Administrator list of every game.
/// </summary>
public class AdminGamesViewModel
{
    [StringLength(GameRules.PlayerNameMaxLength)]
    public string? PlayerFilter { get; set; }
    public List<GameHistoryItem> Games { get; set; } = new();
}

/// <summary>
/// Administrator list of the prizes owed to players.
/// </summary>
/// <param name="Games">List&lt;GameHistoryItem&gt;: finished games with a prize greater than zero.</param>
public record PrizesToPayViewModel(List<GameHistoryItem> Games)
{
    public decimal TotalToPay => Games.Sum(game => game.FinalPrize ?? 0);
}
