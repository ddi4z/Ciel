using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuizCiel.Tests.Support;
using QuizCiel.Web;
using QuizCiel.Web.Data;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Services.Abstractions;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Tests.Services;

/// <summary>
/// Player registration with the real ASP.NET Core Identity stack over SQLite in-memory.
/// </summary>
public class AccountServiceTests
{
    private const string ValidPassword = "Clave2026";

    /// <summary>
    /// Registering creates the login account (with hashed password and Player role) and the player together.
    /// </summary>
    [Fact]
    public async Task RegisterPlayer_Valid_CreatesAccountAndPlayer()
    {
        using var database = new SqliteTestDatabase();
        await using var serviceProvider = await BuildIdentityServicesAsync(database);
        await using var requestScope = serviceProvider.CreateAsyncScope();
        var accountService = requestScope.ServiceProvider.GetRequiredService<AccountService>();
        var userManager = requestScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var playerAccount = await accountService.RegisterPlayerAsync("1.012.345.678", "Ana Gómez", ValidPassword, CancellationToken.None);

        Assert.Equal("1012345678", playerAccount.UserName);
        Assert.NotEqual(ValidPassword, playerAccount.PasswordHash);
        Assert.True(await userManager.IsInRoleAsync(playerAccount, AppRoles.Player));
        await using var readContext = database.CreateContext();
        Assert.Equal(playerAccount.Id, readContext.Players.Single().UserId);
    }

    /// <summary>
    /// A cédula can be registered only once.
    /// </summary>
    [Fact]
    public async Task RegisterPlayer_DuplicateDocument_IsRejected()
    {
        using var database = new SqliteTestDatabase();
        await using var serviceProvider = await BuildIdentityServicesAsync(database);
        await RegisterInNewScopeAsync(serviceProvider, "1012345678", "Ana Gómez", ValidPassword);

        await Assert.ThrowsAsync<DomainException>(() =>
            RegisterInNewScopeAsync(serviceProvider, "1012345678", "Otra Persona", ValidPassword));
    }

    /// <summary>
    /// A weak password is rejected with a Spanish message and leaves neither account nor player behind.
    /// </summary>
    [Fact]
    public async Task RegisterPlayer_WeakPassword_IsRejectedWithoutLeftovers()
    {
        using var database = new SqliteTestDatabase();
        await using var serviceProvider = await BuildIdentityServicesAsync(database);

        var rejection = await Assert.ThrowsAsync<DomainException>(() =>
            RegisterInNewScopeAsync(serviceProvider, "1012345678", "Ana Gómez", "corta"));

        Assert.Contains("contraseña", rejection.Message);
        await using var readContext = database.CreateContext();
        Assert.Empty(readContext.Users);
        Assert.Empty(readContext.Players);
    }

    /// <summary>
    /// A cédula typed with dots or spaces is converted into the account username; other text is kept.
    /// </summary>
    [Theory]
    [InlineData("1.012.345.678", "1012345678")]
    [InlineData(" 79 123 456 ", "79123456")]
    [InlineData(" admin ", "admin")]
    public void NormalizeLoginName_ConvertsTypedLogin(string typedLogin, string expectedUserName)
    {
        Assert.Equal(expectedUserName, AccountService.NormalizeLoginName(typedLogin));
    }

    /// <summary>
    /// Registers a player in its own simulated request.
    /// </summary>
    /// <param name="serviceProvider">ServiceProvider: container with Identity configured.</param>
    /// <param name="documentNumber">string: cédula.</param>
    /// <param name="fullName">string: full name.</param>
    /// <param name="password">string: password.</param>
    /// <returns>Task: completes when registered.</returns>
    private static async Task RegisterInNewScopeAsync(ServiceProvider serviceProvider, string documentNumber, string fullName, string password)
    {
        await using var requestScope = serviceProvider.CreateAsyncScope();
        var accountService = requestScope.ServiceProvider.GetRequiredService<AccountService>();
        await accountService.RegisterPlayerAsync(documentNumber, fullName, password, CancellationToken.None);
    }

    /// <summary>
    /// Builds a container with Identity configured exactly like the application, over the test database,
    /// and creates the roles.
    /// </summary>
    /// <param name="database">SqliteTestDatabase: test database.</param>
    /// <returns>Task&lt;ServiceProvider&gt;: ready container.</returns>
    private static async Task<ServiceProvider> BuildIdentityServicesAsync(SqliteTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<QuizDbContext>(options => options.UseSqlite(database.Connection));
        services.AddIdentityCore<IdentityUser>(DependencyInjection.ConfigureIdentityOptions)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<QuizDbContext>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddSingleton<IClock>(database.Clock);
        services.AddScoped<AccountService>();

        var serviceProvider = services.BuildServiceProvider();
        await using var setupScope = serviceProvider.CreateAsyncScope();
        var roleManager = setupScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in AppRoles.All)
            await roleManager.CreateAsync(new IdentityRole(roleName));
        return serviceProvider;
    }
}
