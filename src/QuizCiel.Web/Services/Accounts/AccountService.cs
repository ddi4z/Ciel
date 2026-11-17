using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Services.Abstractions;

namespace QuizCiel.Web.Services.Accounts;

/// <summary>
/// Use case "Register as a player": creates the login account (username = cédula, hashed password) and
/// the player record together, so neither can exist without the other.
/// </summary>
public partial class AccountService
{
    private readonly UserManager<IdentityUser> userManager;
    private readonly IPlayerRepository playerRepository;
    private readonly IClock clock;
    private readonly ILogger<AccountService> logger;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="userManager">UserManager&lt;IdentityUser&gt;: Identity account store.</param>
    /// <param name="playerRepository">IPlayerRepository: player data access.</param>
    /// <param name="clock">IClock: current time.</param>
    /// <param name="logger">ILogger&lt;AccountService&gt;: records registrations (no personal data).</param>
    public AccountService(UserManager<IdentityUser> userManager, IPlayerRepository playerRepository, IClock clock, ILogger<AccountService> logger)
    {
        this.userManager = userManager;
        this.playerRepository = playerRepository;
        this.clock = clock;
        this.logger = logger;
    }

    /// <summary>
    /// Registers a player. Business rule: one account per cédula.
    /// </summary>
    /// <param name="documentNumber">string: cédula as typed.</param>
    /// <param name="fullName">string: full name as typed.</param>
    /// <param name="password">string: password that must satisfy the Identity policy.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;IdentityUser&gt;: the new login account, ready to sign in.</returns>
    public async Task<IdentityUser> RegisterPlayerAsync(string documentNumber, string fullName, string password, CancellationToken cancellationToken)
    {
        var normalizedDocument = Player.NormalizeDocumentNumber(documentNumber);
        var normalizedName = Player.NormalizeFullName(fullName);
        if (await playerRepository.IsDocumentRegisteredAsync(normalizedDocument, cancellationToken))
            throw new DomainException("Ya existe una cuenta con esa cédula.");

        var playerAccount = new IdentityUser(normalizedDocument);
        var newPlayer = Player.Create(playerAccount.Id, normalizedDocument, normalizedName, clock.GetUtcNow());
        await playerRepository.RunInTransactionAsync(async () =>
        {
            (await userManager.CreateAsync(playerAccount, password)).ThrowIfFailed();
            (await userManager.AddToRoleAsync(playerAccount, AppRoles.Player)).ThrowIfFailed();
            playerRepository.Add(newPlayer);
            await playerRepository.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        logger.LogInformation("Player {PlayerId} registered with account {UserId}.", newPlayer.Id, playerAccount.Id);
        return playerAccount;
    }

    /// <summary>
    /// Converts what the user typed in the login form into the account username: a cédula typed with dots or
    /// spaces becomes digits only; any other text (the administrator username) is only trimmed.
    /// </summary>
    /// <param name="typedLogin">string: cédula or username as typed.</param>
    /// <returns>string: username to look up.</returns>
    public static string NormalizeLoginName(string typedLogin)
    {
        var trimmedLogin = typedLogin?.Trim() ?? string.Empty;
        return LooksLikeDocumentPattern().IsMatch(trimmedLogin)
            ? Player.StripDocumentSeparators(trimmedLogin)
            : trimmedLogin;
    }

    /// <summary>
    /// Matches text made only of digits and common cédula separators.
    /// </summary>
    /// <returns>Regex: compiled pattern.</returns>
    [GeneratedRegex(@"^[\d.\s\-]+$")]
    private static partial Regex LooksLikeDocumentPattern();
}
