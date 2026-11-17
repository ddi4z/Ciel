using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Data;

/// <summary>
/// Entity Framework Core context, including the ASP.NET Core Identity tables (accounts, roles, logins).
/// </summary>
public class QuizDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<RoundConfig> RoundConfigs => Set<RoundConfig>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();

    /// <summary>
    /// Creates the context with the options configured in dependency injection.
    /// </summary>
    /// <param name="options">DbContextOptions&lt;QuizDbContext&gt;: provider and connection settings.</param>
    public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Maps the Identity tables, applies every <see cref="IEntityTypeConfiguration{TEntity}"/> declared in this
    /// assembly and stores all dates as UTC.
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder: EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuizDbContext).Assembly);
        UtcDateTimeConvention.Apply(modelBuilder);
    }

    /// <summary>
    /// Saves all pending changes in one transaction, renewing the concurrency stamp of modified games and
    /// translating optimistic concurrency failures into a user-facing exception.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">bool: whether to accept tracked changes after saving.</param>
    /// <param name="cancellationToken">CancellationToken: cancels the operation.</param>
    /// <returns>Task&lt;int&gt;: number of affected rows.</returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var modifiedGames = ChangeTracker.Entries<Game>().Where(entry => entry.State == EntityState.Modified);
        foreach (var modifiedGame in modifiedGames)
            modifiedGame.Entity.RenewConcurrencyStamp();

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException concurrencyError)
        {
            throw new ConcurrencyConflictException("La partida fue actualizada desde otra pestaña o solicitud. Recarga la página.", concurrencyError);
        }
    }
}
