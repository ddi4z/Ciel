using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web;

/// <summary>
/// One-off tasks executed when the application starts.
/// </summary>
public static class StartupTasks
{
    /// <summary>
    /// Creates the roles and the bootstrap administrator. A database that is not reachable yet is logged
    /// instead of stopping the application, so the error page explains the problem.
    /// </summary>
    /// <param name="application">WebApplication: the built application.</param>
    /// <returns>Task: completes when seeding finished or failed.</returns>
    public static async Task SeedAccountsAsync(this WebApplication application)
    {
        await using var startupScope = application.Services.CreateAsyncScope();
        var accountSeeder = startupScope.ServiceProvider.GetRequiredService<AccountSeeder>();
        try
        {
            await accountSeeder.SeedAsync();
        }
        catch (Exception seedingError)
        {
            application.Logger.LogError(seedingError, "Could not create the roles and the administrator account.");
        }
    }
}
