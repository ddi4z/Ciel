using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;

namespace QuizCiel.Web.Data.Configurations;

/// <summary>
/// Maps <see cref="Category"/> to the Category table.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>
    /// Configures columns, the unique level and the relation with questions.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;Category&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(GameRules.CategoryNameMaxLength).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(GameRules.CategoryDescriptionMaxLength);
        builder.HasIndex(category => category.Level).IsUnique();
        builder.HasMany(category => category.Questions)
            .WithOne(question => question.Category)
            .HasForeignKey(question => question.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Maps <see cref="Question"/> to the Question table.
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    /// <summary>
    /// Configures columns, the lookup index and the relation with options.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;Question&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Question");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Text).HasMaxLength(GameRules.QuestionTextMaxLength).IsRequired();
        builder.HasIndex(question => new { question.CategoryId, question.IsActive });
        builder.HasMany(question => question.Options)
            .WithOne()
            .HasForeignKey(option => option.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Maps <see cref="AnswerOption"/> to the AnswerOption table.
/// </summary>
public class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    /// <summary>
    /// Configures columns, one option per position and the filtered unique index that allows a single
    /// correct option per question.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;AnswerOption&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ToTable("AnswerOption");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Text).HasMaxLength(GameRules.OptionTextMaxLength).IsRequired();
        builder.HasIndex(option => new { option.QuestionId, option.Position })
            .IsUnique()
            .HasDatabaseName("UX_AnswerOption_QuestionId_Position");
        builder.HasIndex(option => option.QuestionId)
            .IsUnique()
            .HasFilter("[IsCorrect] = 1")
            .HasDatabaseName("UX_AnswerOption_OneCorrectPerQuestion");
    }
}

/// <summary>
/// Maps <see cref="RoundConfig"/> to the RoundConfig table.
/// </summary>
public class RoundConfigConfiguration : IEntityTypeConfiguration<RoundConfig>
{
    /// <summary>
    /// Configures the round number as natural key and the prize as whole pesos.
    /// </summary>
    /// <param name="builder">EntityTypeBuilder&lt;RoundConfig&gt;: entity builder.</param>
    public void Configure(EntityTypeBuilder<RoundConfig> builder)
    {
        builder.ToTable("RoundConfig");
        builder.HasKey(roundConfig => roundConfig.RoundNumber);
        builder.Property(roundConfig => roundConfig.RoundNumber).ValueGeneratedNever();
        builder.Property(roundConfig => roundConfig.Prize).HasPrecision(18, 0);
    }
}
