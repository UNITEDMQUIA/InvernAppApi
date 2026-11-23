using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvernApp.Api.Data;

internal static class UtcDate
{
    public static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime MarkUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

/// <summary>
/// Guarda DateTime en UTC y al leer lo marca como UTC (el JSON saldrá con "Z").
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => UtcDate.ToUtc(v), v => UtcDate.MarkUtc(v))
    {
    }
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue ? UtcDate.ToUtc(v.Value) : (DateTime?)null,
            v => v.HasValue ? UtcDate.MarkUtc(v.Value) : (DateTime?)null)
    {
    }
}