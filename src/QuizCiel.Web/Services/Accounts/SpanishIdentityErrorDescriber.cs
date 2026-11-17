using Microsoft.AspNetCore.Identity;
using QuizCiel.Web.Domain;

namespace QuizCiel.Web.Services.Accounts;

/// <summary>
/// Spanish texts for the ASP.NET Core Identity validation errors shown to users.
/// </summary>
public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <inheritdoc />
    public override IdentityError DefaultError()
    {
        return new IdentityError { Code = nameof(DefaultError), Description = "No fue posible completar la operación." };
    }

    /// <inheritdoc />
    public override IdentityError DuplicateUserName(string userName)
    {
        return new IdentityError { Code = nameof(DuplicateUserName), Description = "Ya existe una cuenta con esa cédula." };
    }

    /// <inheritdoc />
    public override IdentityError InvalidUserName(string? userName)
    {
        return new IdentityError { Code = nameof(InvalidUserName), Description = "El usuario contiene caracteres no permitidos." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordTooShort(int length)
    {
        return new IdentityError { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordRequiresDigit()
    {
        return new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe incluir al menos un número." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordRequiresLower()
    {
        return new IdentityError { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe incluir al menos una letra minúscula." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUpper()
    {
        return new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe incluir al menos una letra mayúscula." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe incluir al menos un símbolo." };
    }

    /// <inheritdoc />
    public override IdentityError PasswordMismatch()
    {
        return new IdentityError { Code = nameof(PasswordMismatch), Description = "La contraseña no es correcta." };
    }

    /// <summary>
    /// Describes the password policy in one sentence, for help texts in forms.
    /// </summary>
    /// <returns>string: policy description.</returns>
    public static string DescribePasswordPolicy()
    {
        return $"Mínimo {GameRules.PasswordMinLength} caracteres, con mayúscula, minúscula y número.";
    }
}
