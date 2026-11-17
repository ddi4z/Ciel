namespace QuizCiel.Web.Domain.Exceptions;

/// <summary>
/// Raised when a requested resource does not exist. Translated to an HTTP 404 by the error middleware.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Creates the exception describing the missing resource.
    /// </summary>
    /// <param name="message">string: description of the resource that was not found.</param>
    public NotFoundException(string message) : base(message)
    {
    }
}
