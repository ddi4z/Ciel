using QuizCiel.Tests.Support;
using QuizCiel.Web.Domain.Entities;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Tests.Domain;

/// <summary>
/// Business rules of players and categories.
/// </summary>
public class PlayerAndCategoryTests
{
    /// <summary>
    /// The cédula keeps only digits and the name is trimmed with repeated spaces collapsed.
    /// </summary>
    [Fact]
    public void Player_DocumentAndName_AreNormalized()
    {
        var player = Player.Create("user-1", "1.012.345 678", "  María   José ", TestData.GameStartUtc);

        Assert.Equal("1012345678", player.DocumentNumber);
        Assert.Equal("María José", player.FullName);
    }

    /// <summary>
    /// A cédula with letters, too few digits or too many digits is rejected.
    /// </summary>
    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("10A2345678")]
    [InlineData("")]
    public void Player_InvalidDocument_IsRejected(string invalidDocumentNumber)
    {
        Assert.Throws<DomainException>(() => Player.Create("user-1", invalidDocumentNumber, "Ana Gómez", TestData.GameStartUtc));
    }

    /// <summary>
    /// Too short names and names with digits or symbols are rejected.
    /// </summary>
    [Theory]
    [InlineData("Al")]
    [InlineData("<script>")]
    [InlineData("Ana 123")]
    public void Player_InvalidName_IsRejected(string invalidName)
    {
        Assert.Throws<DomainException>(() => Player.Create("user-1", "1012345678", invalidName, TestData.GameStartUtc));
    }

    /// <summary>
    /// A player without a login account is rejected.
    /// </summary>
    [Fact]
    public void Player_WithoutAccount_IsRejected()
    {
        Assert.Throws<DomainException>(() => Player.Create(" ", "1012345678", "Ana Gómez", TestData.GameStartUtc));
    }

    /// <summary>
    /// A player who already has a game cannot start another one.
    /// </summary>
    [Fact]
    public void Player_WithOneGame_CannotStartAnother()
    {
        Player.EnsureCanStartGame(hasAlreadyPlayed: false);

        Assert.Throws<DomainException>(() => Player.EnsureCanStartGame(hasAlreadyPlayed: true));
    }

    /// <summary>
    /// Category levels outside 1 to 5 are rejected.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Category_WithLevelOutOfRange_IsRejected(int invalidLevel)
    {
        Assert.Throws<DomainException>(() => Category.Create("Historia", invalidLevel, null));
    }

    /// <summary>
    /// A category with questions cannot be deleted.
    /// </summary>
    [Fact]
    public void Category_WithQuestions_CannotBeDeleted()
    {
        Assert.Throws<DomainException>(() => Category.EnsureCanBeDeleted(questionCount: 1));
    }
}
