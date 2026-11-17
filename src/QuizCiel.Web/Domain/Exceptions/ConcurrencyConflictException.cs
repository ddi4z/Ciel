namespace QuizCiel.Web.Domain.Exceptions;

/// <summary>
/// Raised when the same record was modified by another request (for example a double click or a second tab).
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    /// <summary>
    /// Creates the exception with a user-facing message.
    /// </summary>
    /// <param name="message">string: explanation of the conflict, in the UI language.</param>
    /// <param name="originalError">Exception: error raised by the data layer, kept for the logs.</param>
    public ConcurrencyConflictException(string message, Exception originalError) : base(message, originalError)
    {
    }
}
