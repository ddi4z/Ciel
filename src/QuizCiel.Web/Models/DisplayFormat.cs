using System.Globalization;
using QuizCiel.Web.Domain.Enums;

namespace QuizCiel.Web.Models;

/// <summary>
/// Single place where values are formatted for the UI (Colombian pesos, local time and status labels).
/// </summary>
public static class DisplayFormat
{
    private static readonly CultureInfo ColombianCulture = CultureInfo.GetCultureInfo("es-CO");
    private static readonly TimeZoneInfo ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    /// <summary>
    /// Formats an amount as Colombian pesos without decimals, for example "$ 2.000.000".
    /// </summary>
    /// <param name="amount">decimal: amount in pesos.</param>
    /// <returns>string: formatted amount.</returns>
    public static string Money(decimal amount)
    {
        return amount.ToString("C0", ColombianCulture);
    }

    /// <summary>
    /// Converts a UTC instant to Colombia time and formats it as day/month/year hour:minute.
    /// </summary>
    /// <param name="instantUtc">DateTime: instant in UTC.</param>
    /// <returns>string: formatted local date and time.</returns>
    public static string LocalDateTime(DateTime instantUtc)
    {
        var colombiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc), ColombiaTimeZone);
        return colombiaTime.ToString("dd/MM/yyyy HH:mm", ColombianCulture);
    }

    /// <summary>
    /// Hides all but the last four digits of a cédula, for screens where the full number is not needed.
    /// </summary>
    /// <param name="documentNumber">string: normalized cédula.</param>
    /// <returns>string: masked value, for example "******5678".</returns>
    public static string MaskedDocument(string documentNumber)
    {
        const int VisibleDigits = 4;
        if (documentNumber.Length <= VisibleDigits)
            return documentNumber;
        return new string('*', documentNumber.Length - VisibleDigits) + documentNumber[^VisibleDigits..];
    }

    /// <summary>
    /// Returns the Spanish label of a game status.
    /// </summary>
    /// <param name="status">GameStatus: status to describe.</param>
    /// <returns>string: label shown to the user.</returns>
    public static string StatusLabel(GameStatus status)
    {
        return status switch
        {
            GameStatus.InProgress => "En curso",
            GameStatus.Won => "Ganó",
            GameStatus.Withdrawn => "Se retiró",
            GameStatus.Lost => "Perdió",
            GameStatus.ForcedEnd => "Tiempo agotado",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Returns the CSS modifier used to color a game status; the colors themselves live in theme.css.
    /// </summary>
    /// <param name="status">GameStatus: status to style.</param>
    /// <returns>string: CSS class name.</returns>
    public static string StatusCssClass(GameStatus status)
    {
        return status switch
        {
            GameStatus.Won => "status-badge--success",
            GameStatus.Withdrawn => "status-badge--info",
            GameStatus.Lost or GameStatus.ForcedEnd => "status-badge--danger",
            _ => "status-badge--neutral"
        };
    }
}
