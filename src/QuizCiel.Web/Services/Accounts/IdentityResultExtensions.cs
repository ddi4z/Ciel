using Microsoft.AspNetCore.Identity;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Services.Accounts;

/// <summary>
/// Helpers shared by the services that create Identity accounts and roles.
/// </summary>
public static class IdentityResultExtensions
{
    /// <summary>
    /// Fails with the Identity error descriptions (already in Spanish) when an operation did not succeed.
    /// </summary>
    /// <param name="identityResult">IdentityResult: result to check.</param>
    public static void ThrowIfFailed(this IdentityResult identityResult)
    {
        if (!identityResult.Succeeded)
            throw new DomainException(string.Join(" ", identityResult.Errors.Select(error => error.Description)));
    }
}
