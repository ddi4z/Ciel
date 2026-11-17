using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Configurations;

/// <summary>
/// Maps <see cref="Player"/> to the Player table.
/// </summary>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    /// <summary>
    /// Configures columns, the unique cédula and the one-to-one link with the login account.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;Player&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Player");
        builder.HasKey(player => player.Id);
        builder.Property(player => player.FullName).HasMaxLength(GameRules.PlayerNameMaxLength).IsRequired();
        builder.Property(player => player.DocumentNumber)
            .HasMaxLength(GameRules.DocumentNumberMaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(player => player.DocumentNumber).IsUnique();
        builder.HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<Player>(player => player.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Maps <see cref="Game"/> to the Game table.
/// </summary>
public class GameConfigurationMapping : IEntityTypeConfiguration<Game>
{
    /// <summary>
    /// Configures columns, the optimistic concurrency token, one game per player and the relations with
    /// player and rounds.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;Game&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Game");
        builder.HasKey(game => game.Id);
        builder.Property(game => game.Id).ValueGeneratedNever();
        builder.Property(game => game.AccumulatedPrize).HasPrecision(18, 0);
        builder.Property(game => game.FinalPrize).HasPrecision(18, 0);
        builder.Property(game => game.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasIndex(game => game.StartedAt);
        builder.HasIndex(game => game.PlayerId).IsUnique().HasDatabaseName("UX_Game_PlayerId");
        builder.HasOne(game => game.Player)
            .WithMany()
            .HasForeignKey(game => game.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(game => game.Rounds)
            .WithOne()
            .HasForeignKey(round => round.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="GameRound"/> to the GameRound table.
/// </summary>
public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    /// <summary>
    /// Configures columns, one question per round of a game and the references to question and chosen option.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;GameRound&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable("GameRound");
        builder.HasKey(round => round.Id);
        builder.Property(round => round.Prize).HasPrecision(18, 0);
        builder.HasIndex(round => new { round.GameId, round.RoundNumber }).IsUnique();
        builder.HasOne(round => round.Question)
            .WithMany()
            .HasForeignKey(round => round.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnswerOption>()
            .WithMany()
            .HasForeignKey(round => round.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
