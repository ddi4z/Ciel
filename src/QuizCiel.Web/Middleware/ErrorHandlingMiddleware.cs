using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuizCiel.Web.Domain.Exceptions;

namespace QuizCiel.Web.Middleware;

/// <summary>
/// Last link of the error-handling chain: logs unhandled exceptions and turns them into an HTTP status code,
/// which the status code pages middleware then renders as a friendly error page.
/// </summary>
public class ErrorHandlingMiddleware
{
    private const int SqlServerUniqueIndexViolation = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;

    private readonly RequestDelegate nextMiddleware;
    private readonly ILogger<ErrorHandlingMiddleware> logger;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="nextMiddleware">RequestDelegate: next component of the pipeline.</param>
    /// <param name="logger">ILogger&lt;ErrorHandlingMiddleware&gt;: logger.</param>
    public ErrorHandlingMiddleware(RequestDelegate nextMiddleware, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.nextMiddleware = nextMiddleware;
        this.logger = logger;
    }

    /// <summary>
    /// Runs the rest of the pipeline and maps any escaped exception to 404, 409 or 500. Only a unique key
    /// violation (two requests creating the same record at once) is a 409; any other database error is a
    /// server error, so real bugs are not hidden as conflicts.
    /// </summary>
    /// <param name="httpContext">HttpContext: current request.</param>
    /// <returns>Task: completes when the request was handled.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await nextMiddleware(httpContext);
        }
        catch (Exception unhandledException) when (!httpContext.Response.HasStarted)
        {
            var statusCode = unhandledException switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                DomainException => StatusCodes.Status409Conflict,
                DbUpdateException databaseError when IsUniqueKeyViolation(databaseError) => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                logger.LogError(unhandledException, "Unhandled error processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            else
                logger.LogWarning(unhandledException, "Request {Method} {Path} ended with {StatusCode}", httpContext.Request.Method, httpContext.Request.Path, statusCode);

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Tells whether SQL Server rejected the save because of a unique index or unique constraint.
    /// </summary>
    /// <param name="databaseError">DbUpdateException: error raised by EF Core.</param>
    /// <returns>bool: true for a duplicate key.</returns>
    private static bool IsUniqueKeyViolation(DbUpdateException databaseError)
    {
        return databaseError.InnerException is SqlException { Number: SqlServerUniqueIndexViolation or SqlServerUniqueConstraintViolation };
    }
}
