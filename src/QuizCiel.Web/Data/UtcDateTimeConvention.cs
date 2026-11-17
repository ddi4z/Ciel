using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace QuizCiel.Web.Data;

/// <summary>
/// Keeps every stored date in UTC: dates read from the database are marked as UTC, because SQL Server
/// <c>datetime2</c> does not store the kind, and a local date written by mistake is converted before saving.
/// </summary>
public static class UtcDateTimeConvention
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value, storedValue => DateTime.SpecifyKind(storedValue, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter =
        new(value => value.HasValue && value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime() : value, storedValue => storedValue.HasValue ? DateTime.SpecifyKind(storedValue.Value, DateTimeKind.Utc) : null);

    /// <summary>
    /// Attaches the UTC converters to every DateTime property of the model.
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder: EF Core model builder.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        var dateTimeProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?));

        foreach (var property in dateTimeProperties)
        {
            if (property.ClrType == typeof(DateTime))
                property.SetValueConverter(UtcConverter);
            else
                property.SetValueConverter(NullableUtcConverter);
        }
    }
}
