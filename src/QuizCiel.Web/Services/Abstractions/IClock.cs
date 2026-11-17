namespace QuizCiel.Web.Services.Abstractions;

/// <summary>
/// Source of the current time. Abstracted so time-dependent rules (answer deadline) can be tested.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Returns the current instant.
    /// </summary>
    /// <returns>DateTime: current instant in UTC.</returns>
    DateTime GetUtcNow();
}

/// <summary>
/// Production clock backed by the system time.
/// </summary>
public class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
}
