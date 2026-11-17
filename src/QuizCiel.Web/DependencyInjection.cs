using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Data;
using QuizCiel.Web.Data.Repositories;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Services;
using QuizCiel.Web.Services.Abstractions;
using QuizCiel.Web.Services.Accounts;
using QuizCiel.Web.Services.Strategies;

namespace QuizCiel.Web;

/// <summary>
/// Registers the application components in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    private const string ConnectionStringName = "QuizDb";
    private const int MaximumFailedSignInAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(2);

    /// <summary>
    /// Registers the database context and the repositories.
    /// </summary>
    /// <param name="services">IServiceCollection: container being configured.</param>
    /// <param name="configuration">IConfiguration: application configuration with the connection string.</param>
    /// <returns>IServiceCollection: the same container, for chaining.</returns>
    public static IServiceCollection AddQuizData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{ConnectionStringName}'. Defínela con la variable de entorno " +
                $"ConnectionStrings__{ConnectionStringName} o con dotnet user-secrets.");

        services.AddDbContext<QuizDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IRoundConfigRepository, RoundConfigRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        return services;
    }

    /// <summary>
    /// Registers the service layer, the question selection strategy and the clock.
    /// </summary>
    /// <param name="services">IServiceCollection: container being configured.</param>
    /// <param name="configuration">IConfiguration: application configuration with the bootstrap administrator.</param>
    /// <returns>IServiceCollection: the same container, for chaining.</returns>
    public static IServiceCollection AddQuizServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminCredentialsOptions>(configuration.GetSection(AdminCredentialsOptions.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(Random.Shared);
        services.AddScoped<IQuestionSelector, RandomQuestionSelector>();
        services.AddScoped<GameReadinessService>();
        services.AddScoped<GameService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<RoundSettingsService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AccountSeeder>();
        return services;
    }

    /// <summary>
    /// Registers ASP.NET Core Identity (hashed passwords, roles, lockout) and its authentication cookie.
    /// </summary>
    /// <param name="services">IServiceCollection: container being configured.</param>
    /// <returns>IServiceCollection: the same container, for chaining.</returns>
    public static IServiceCollection AddQuizIdentity(this IServiceCollection services)
    {
        services.AddIdentity<IdentityUser, IdentityRole>(ConfigureIdentityOptions)
            .AddEntityFrameworkStores<QuizDbContext>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/error/403";
            options.Cookie.Name = "QuizCiel.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = SessionDuration;
            options.SlidingExpiration = true;
        });
        return services;
    }

    /// <summary>
    /// Password policy, lockout after repeated failures and username rules.
    /// </summary>
    /// <param name="options">IdentityOptions: options being configured.</param>
    public static void ConfigureIdentityOptions(IdentityOptions options)
    {
        options.Password.RequiredLength = GameRules.PasswordMinLength;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = MaximumFailedSignInAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    }
}
