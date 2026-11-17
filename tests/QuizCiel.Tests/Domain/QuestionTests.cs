using QuizCiel.Tests.Support;
using QuizCiel.Web.Domain;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Tests.Domain;

/// <summary>
/// Business rules of questions and their options.
/// </summary>
public class QuestionTests
{
    /// <summary>
    /// A question with four distinct options and one correct answer is created active.
    /// </summary>
    [Fact]
    public void Create_WithValidOptions_IsActive()
    {
        var question = Question.Create(1, "¿Capital de Colombia?", TestData.ValidOptionDrafts());

        Assert.True(question.IsActive);
        Assert.Equal(GameRules.OptionsPerQuestion, question.Options.Count);
    }

    /// <summary>
    /// Fewer than four options is rejected.
    /// </summary>
    [Fact]
    public void Create_WithThreeOptions_IsRejected()
    {
        var threeOptionDrafts = TestData.ValidOptionDrafts().Take(3).ToList();

        Assert.Throws<DomainException>(() => Question.Create(1, "Enunciado", threeOptionDrafts));
    }

    /// <summary>
    /// More than one correct answer is rejected.
    /// </summary>
    [Fact]
    public void Create_WithTwoCorrectOptions_IsRejected()
    {
        var optionDrafts = TestData.ValidOptionDrafts();
        optionDrafts[1] = optionDrafts[1] with { IsCorrect = true };

        Assert.Throws<DomainException>(() => Question.Create(1, "Enunciado", optionDrafts));
    }

    /// <summary>
    /// No correct answer is rejected.
    /// </summary>
    [Fact]
    public void Create_WithoutCorrectOption_IsRejected()
    {
        var optionDrafts = TestData.ValidOptionDrafts();
        optionDrafts[0] = optionDrafts[0] with { IsCorrect = false };

        Assert.Throws<DomainException>(() => Question.Create(1, "Enunciado", optionDrafts));
    }

    /// <summary>
    /// Repeated option texts (ignoring case) are rejected.
    /// </summary>
    [Fact]
    public void Create_WithDuplicatedOptions_IsRejected()
    {
        var optionDrafts = TestData.ValidOptionDrafts();
        optionDrafts[3] = optionDrafts[3] with { Text = optionDrafts[2].Text.ToUpperInvariant() };

        Assert.Throws<DomainException>(() => Question.Create(1, "Enunciado", optionDrafts));
    }

    /// <summary>
    /// A question that was never played can be edited, including which option is correct.
    /// </summary>
    [Fact]
    public void Edit_NotPlayed_UpdatesTextAndCorrectOption()
    {
        var question = TestData.QuestionForLevel(1);
        var editedDrafts = TestData.ValidOptionDrafts("E")
            .Select((draft, position) => draft with { IsCorrect = position == 2 })
            .ToList();

        question.Edit("Nuevo enunciado", editedDrafts, hasBeenPlayed: false);

        Assert.Equal("Nuevo enunciado", question.Text);
        Assert.Equal(editedDrafts[2].Text, question.GetCorrectOption().Text);
    }

    /// <summary>
    /// A question that was already played cannot be edited.
    /// </summary>
    [Fact]
    public void Edit_AlreadyPlayed_IsRejected()
    {
        var question = TestData.QuestionForLevel(1);

        Assert.Throws<DomainException>(() => question.Edit("Otro", TestData.ValidOptionDrafts(), hasBeenPlayed: true));
    }

    /// <summary>
    /// Deactivating is rejected when the category would drop below the minimum of active questions.
    /// </summary>
    [Fact]
    public void Deactivate_AtMinimumActiveQuestions_IsRejected()
    {
        var question = TestData.QuestionForLevel(1);

        Assert.Throws<DomainException>(() => question.Deactivate(GameRules.MinimumActiveQuestionsPerCategory));
    }

    /// <summary>
    /// Deactivating is allowed while the category keeps the minimum of active questions.
    /// </summary>
    [Fact]
    public void Deactivate_AboveMinimumActiveQuestions_Deactivates()
    {
        var question = TestData.QuestionForLevel(1);

        question.Deactivate(GameRules.MinimumActiveQuestionsPerCategory + 1);

        Assert.False(question.IsActive);
    }
}
