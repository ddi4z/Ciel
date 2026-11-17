namespace QuizCiel.Web.Domain.Exceptions;

/// <summary>
/// Raised when an operation violates a business rule. Its message is safe to show to the user.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Creates the exception with a user-facing message.
    /// </summary>
    /// <param name="message">string: description of the violated rule, in the UI language.</param>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with a user-facing message and the technical error that caused it.
    /// </summary>
    /// <param name="message">string: description of the violated rule, in the UI language.</param>
    /// <param name="originalError">Exception: underlying error, kept for the logs.</param>
    public DomainException(string message, Exception originalError) : base(message, originalError)
    {
    }
}
