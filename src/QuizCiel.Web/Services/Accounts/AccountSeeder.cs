using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace QuizCiel.Web.Services.Accounts;

/// <summary>
/// Bootstrap administrator account read from configuration (section "AdminCredentials").
/// It is only used to create the account the first time; the password is then stored hashed by Identity.
/// </summary>
public class AdminCredentialsOptions
{
    public const string SectionName = "AdminCredentials";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Creates, at startup, the roles and the first administrator account when they do not exist yet.
/// </summary>
public class AccountSeeder
{
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly UserManager<IdentityUser> userManager;
    private readonly AdminCredentialsOptions adminCredentials;
    private readonly ILogger<AccountSeeder> logger;

    /// <summary>
    /// Creates the seeder.
    /// </summary>
    /// <param name="roleManager">RoleManager&lt;IdentityRole&gt;: role store.</param>
    /// <param name="userManager">UserManager&lt;IdentityUser&gt;: account store.</param>
    /// <param name="adminCredentialsOptions">IOptions&lt;AdminCredentialsOptions&gt;: bootstrap administrator.</param>
    /// <param name="logger">ILogger&lt;AccountSeeder&gt;: logger.</param>
    public AccountSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IOptions<AdminCredentialsOptions> adminCredentialsOptions,
        ILogger<AccountSeeder> logger)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
        adminCredentials = adminCredentialsOptions.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Ensures every role exists and that the configured administrator account exists. An existing
    /// administrator is never modified, so changing the configuration later does not reset its password.
    /// </summary>
    /// <returns>Task: completes when the accounts are ready.</returns>
    public async Task SeedAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                (await roleManager.CreateAsync(new IdentityRole(roleName))).ThrowIfFailed();
        }

        if (string.IsNullOrWhiteSpace(adminCredentials.Username) || string.IsNullOrWhiteSpace(adminCredentials.Password))
        {
            logger.LogWarning("AdminCredentials is not configured: no administrator account was created.");
            return;
        }
        if (await userManager.FindByNameAsync(adminCredentials.Username) is not null)
            return;

        var administratorAccount = new IdentityUser(adminCredentials.Username);
        (await userManager.CreateAsync(administratorAccount, adminCredentials.Password)).ThrowIfFailed();
        (await userManager.AddToRoleAsync(administratorAccount, AppRoles.Admin)).ThrowIfFailed();
        logger.LogInformation("Administrator account {Username} created.", adminCredentials.Username);
    }
}
