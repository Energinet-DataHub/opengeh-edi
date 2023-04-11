using System.Globalization;
using NodaTime;

namespace Domain.Transactions.Aggregations;

public record Period(Instant Start, Instant End)
{
    public string StartToString()
    {
        return ParsePeriodDateFrom(Start);
    }

    public string EndToString()
    {
        return ParsePeriodDateFrom(End);
    }

    private static string ParsePeriodDateFrom(Instant instant)
    {
        return instant.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture);
    }
}
