using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Data.ReadModels;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Enums;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Services.Abstractions;
using QuizCiel.Web.Services.Strategies;

namespace QuizCiel.Web.Services;

/// <summary>
/// A registered player and, if they already took part, their game.
/// </summary>
/// <param name="Player">Player: the player.</param>
/// <param name="Game">Game?: their only game, or null if they have not played yet.</param>
public record PlayerParticipation(Player Player, Game? Game);

/// <summary>
/// Service layer for the game use cases. It loads data, delegates every rule to the domain and saves.
/// Every action on a game checks that it belongs to the signed-in player.
/// </summary>
public class GameService
{
    private const int HistoryPageSize = 100;
    private const string GameNotFoundMessage = "La partida no existe.";

    private readonly IGameRepository gameRepository;
    private readonly IPlayerRepository playerRepository;
    private readonly IRoundConfigRepository roundConfigRepository;
    private readonly IQuestionSelector questionSelector;
    private readonly GameReadinessService readinessService;
    private readonly IClock clock;
    private readonly ILogger<GameService> logger;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="gameRepository">IGameRepository: games.</param>
    /// <param name="playerRepository">IPlayerRepository: players.</param>
    /// <param name="roundConfigRepository">IRoundConfigRepository: round settings.</param>
    /// <param name="questionSelector">IQuestionSelector: strategy that picks each round question.</param>
    /// <param name="readinessService">GameReadinessService: checks the game can be started.</param>
    /// <param name="clock">IClock: current time.</param>
    /// <param name="logger">ILogger&lt;GameService&gt;: records the game events (no personal data).</param>
    public GameService(
        IGameRepository gameRepository,
        IPlayerRepository playerRepository,
        IRoundConfigRepository roundConfigRepository,
        IQuestionSelector questionSelector,
        GameReadinessService readinessService,
        IClock clock,
        ILogger<GameService> logger)
    {
        this.gameRepository = gameRepository;
        this.playerRepository = playerRepository;
        this.roundConfigRepository = roundConfigRepository;
        this.questionSelector = questionSelector;
        this.readinessService = readinessService;
        this.clock = clock;
        this.logger = logger;
    }

    /// <summary>
    /// Use case "Start game". Business rule: each player takes part only once.
    /// </summary>
    /// <param name="userId">string: login account of the signed-in player.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;Guid&gt;: identifier of the new game.</returns>
    public async Task<Guid> StartAsync(string userId, CancellationToken cancellationToken)
    {
        var player = await FindPlayerAsync(userId, cancellationToken);
        var existingGameId = await gameRepository.FindGameIdOfPlayerAsync(player.Id, cancellationToken);
        Player.EnsureCanStartGame(hasAlreadyPlayed: existingGameId.HasValue);
        await readinessService.EnsureReadyAsync(cancellationToken);

        var nowUtc = clock.GetUtcNow();
        var firstRoundConfig = await GetRoundConfigAsync(1, cancellationToken);
        var firstQuestion = await questionSelector.SelectForLevelAsync(1, cancellationToken);

        var game = Game.Start(player, firstRoundConfig, firstQuestion, nowUtc);
        gameRepository.Add(game);
        await SaveNewGameAsync(player, cancellationToken);
        logger.LogInformation("Game {GameId} started by player {PlayerId}.", game.Id, player.Id);
        return game.Id;
    }

    /// <summary>
    /// Saves a just started game. Two simultaneous "start" clicks both pass the rule check, and the unique
    /// index on Game(PlayerId) rejects the second insert; that case is reported with the same business message.
    /// </summary>
    /// <param name="player">Player: player who started the game.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when the game is saved.</returns>
    private async Task SaveNewGameAsync(Player player, CancellationToken cancellationToken)
    {
        try
        {
            await gameRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrentGameId = await gameRepository.FindGameIdOfPlayerAsync(player.Id, cancellationToken);
            if (concurrentGameId is null)
                throw;
            Player.EnsureCanStartGame(hasAlreadyPlayed: true);
        }
    }

    /// <summary>
    /// Returns the signed-in player and their game (with the forced end applied if its time ran out).
    /// </summary>
    /// <param name="userId">string: login account of the signed-in player.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;PlayerParticipation&gt;: player and optional game.</returns>
    public async Task<PlayerParticipation> GetParticipationAsync(string userId, CancellationToken cancellationToken)
    {
        var player = await FindPlayerAsync(userId, cancellationToken);
        var gameId = await gameRepository.FindGameIdOfPlayerAsync(player.Id, cancellationToken);
        if (gameId is null)
            return new PlayerParticipation(player, null);

        var game = await LoadAndExpireAsync(gameId.Value, cancellationToken);
        return new PlayerParticipation(player, game);
    }

    /// <summary>
    /// Loads a game for viewing. Only its own player or an administrator may see it.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the viewer.</param>
    /// <param name="viewerIsAdmin">bool: whether the viewer is an administrator.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;Game&gt;: the up-to-date game.</returns>
    public async Task<Game> GetForViewerAsync(Guid gameId, string userId, bool viewerIsAdmin, CancellationToken cancellationToken)
    {
        var game = await LoadAndExpireAsync(gameId, cancellationToken);
        if (!viewerIsAdmin && !game.BelongsTo(userId))
            throw new NotFoundException(GameNotFoundMessage);
        return game;
    }

    /// <summary>
    /// Use cases "Answer question", "Level up" and "Accumulate prize": answers and, if the game goes on,
    /// asks the question of the next round.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the signed-in player.</param>
    /// <param name="optionId">int: identifier of the chosen option.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;AnswerOutcome&gt;: what happened as a result of the answer.</returns>
    public async Task<AnswerOutcome> AnswerAsync(Guid gameId, string userId, int optionId, CancellationToken cancellationToken)
    {
        var game = await FindOwnedGameAsync(gameId, userId, cancellationToken);
        var nowUtc = clock.GetUtcNow();
        var answeredRoundNumber = game.CurrentRoundNumber;

        var answerOutcome = game.Answer(optionId, nowUtc);
        if (answerOutcome == AnswerOutcome.CorrectAndLeveledUp)
        {
            var nextRoundConfig = await GetRoundConfigAsync(game.CurrentRoundNumber, cancellationToken);
            var nextQuestion = await questionSelector.SelectForLevelAsync(game.CurrentRoundNumber, cancellationToken);
            game.AssignQuestion(nextRoundConfig, nextQuestion, nowUtc);
        }

        await gameRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Game {GameId} round {RoundNumber} answered: {AnswerOutcome}. Accumulated {AccumulatedPrize}, final prize {FinalPrize}.",
            game.Id, answeredRoundNumber, answerOutcome, game.AccumulatedPrize, game.FinalPrize);
        return answerOutcome;
    }

    /// <summary>
    /// Use case "Withdraw": the player leaves before answering and keeps the accumulated prize.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the signed-in player.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;GameStatus&gt;: final status of the game.</returns>
    public async Task<GameStatus> WithdrawAsync(Guid gameId, string userId, CancellationToken cancellationToken)
    {
        var game = await FindOwnedGameAsync(gameId, userId, cancellationToken);
        var finalStatus = game.Withdraw(clock.GetUtcNow());
        await gameRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Game {GameId} ended by withdrawal request in round {RoundNumber} with status {Status}, final prize {FinalPrize}.",
            game.Id, game.CurrentRoundNumber, finalStatus, game.FinalPrize);
        return finalStatus;
    }

    /// <summary>
    /// Administrator use case "View games": most recent games, optionally filtered by name or cédula. Games
    /// abandoned after their time ran out are closed first, so none is listed as "in progress" forever.
    /// </summary>
    /// <param name="playerFilter">string?: text contained in the player name or cédula.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;GameHistoryItem&gt;&gt;: games, newest first.</returns>
    public async Task<List<GameHistoryItem>> GetHistoryAsync(string? playerFilter, CancellationToken cancellationToken)
    {
        await ExpireAbandonedGamesAsync(cancellationToken);
        return await gameRepository.GetHistoryAsync(playerFilter, HistoryPageSize, cancellationToken);
    }

    /// <summary>
    /// Applies the forced end to every game whose time ran out while nobody had it open. If a player acts on
    /// one of them at the same moment, that request wins and the rest are closed on the next call.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task: completes when the expired games are saved.</returns>
    private async Task ExpireAbandonedGamesAsync(CancellationToken cancellationToken)
    {
        var nowUtc = clock.GetUtcNow();
        var shortestPossibleDeadline = TimeSpan.FromSeconds(GameRules.MinimumTimeLimitSeconds + GameRules.AnswerGracePeriodSeconds);
        var candidateGames = await gameRepository.FindPossiblyExpiredGamesAsync(nowUtc - shortestPossibleDeadline, cancellationToken);
        var expiredGames = candidateGames.Where(game => game.ExpireIfTimedOut(nowUtc)).ToList();
        if (expiredGames.Count == 0)
            return;

        try
        {
            await gameRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("{ExpiredGameCount} abandoned game(s) ended because their time ran out.", expiredGames.Count);
        }
        catch (ConcurrencyConflictException)
        {
            gameRepository.DiscardPendingChanges();
        }
    }

    /// <summary>
    /// Administrator use case "View prizes to pay": finished games whose player takes home a prize.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;GameHistoryItem&gt;&gt;: games with prize.</returns>
    public Task<List<GameHistoryItem>> GetPrizesToPayAsync(CancellationToken cancellationToken)
    {
        return gameRepository.GetGamesWithPrizeAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the current round settings, used to show the prize ladder.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;List&lt;RoundConfig&gt;&gt;: settings ordered by round.</returns>
    public Task<List<RoundConfig>> GetPrizeLadderAsync(CancellationToken cancellationToken)
    {
        return roundConfigRepository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a game and applies the forced end if its time ran out while nobody was looking. When another
    /// request (for example a second tab) changed the game at the same moment, its version is read again
    /// instead of failing, because a page view must not show a conflict error.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;Game&gt;: the up-to-date game.</returns>
    private async Task<Game> LoadAndExpireAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await FindGameAsync(gameId, cancellationToken);
        if (!game.ExpireIfTimedOut(clock.GetUtcNow()))
            return game;

        try
        {
            await gameRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Game {GameId} ended because round {RoundNumber} timed out.", game.Id, game.CurrentRoundNumber);
            return game;
        }
        catch (ConcurrencyConflictException)
        {
            gameRepository.DiscardPendingChanges();
            return await FindGameAsync(gameId, cancellationToken);
        }
    }

    /// <summary>
    /// Loads a game ready to be played or shown, or fails with "not found".
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Game&gt;: the tracked game.</returns>
    private async Task<Game> FindGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        return await gameRepository.FindForPlayAsync(gameId, cancellationToken)
            ?? throw new NotFoundException(GameNotFoundMessage);
    }

    /// <summary>
    /// Loads a game that must belong to the signed-in player; other players get "not found" so game
    /// identifiers of other people are not revealed.
    /// </summary>
    /// <param name="gameId">Guid: game identifier.</param>
    /// <param name="userId">string: login account of the signed-in player.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Game&gt;: the tracked game.</returns>
    private async Task<Game> FindOwnedGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
    {
        var game = await gameRepository.FindForPlayAsync(gameId, cancellationToken);
        if (game is null || !game.BelongsTo(userId))
            throw new NotFoundException(GameNotFoundMessage);
        return game;
    }

    /// <summary>
    /// Loads the player of a login account or fails when the account is not a player (for example, the administrator).
    /// </summary>
    /// <param name="userId">string: login account identifier.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;Player&gt;: the tracked player.</returns>
    private async Task<Player> FindPlayerAsync(string userId, CancellationToken cancellationToken)
    {
        return await playerRepository.FindByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Solo los jugadores registrados pueden participar.");
    }

    /// <summary>
    /// Loads the settings of a round or fails when the game is not configured.
    /// </summary>
    /// <param name="roundNumber">int: round number.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the query.</param>
    /// <returns>Task&lt;RoundConfig&gt;: the round settings.</returns>
    private async Task<RoundConfig> GetRoundConfigAsync(int roundNumber, CancellationToken cancellationToken)
    {
        return await roundConfigRepository.FindAsync(roundNumber, cancellationToken)
            ?? throw new DomainException($"La ronda {roundNumber} no está configurada.");
    }
}
