using NodaTime;

namespace Domain.Transactions.Aggregations;

public record Period(Instant Start, Instant End);
