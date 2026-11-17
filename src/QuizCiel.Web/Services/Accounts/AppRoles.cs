namespace QuizCiel.Web.Services.Accounts;

/// <summary>
/// Names of the authorization roles of the application.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Player = "Player";

    public static readonly string[] All = [Admin, Player];
}
