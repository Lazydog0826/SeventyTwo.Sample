using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Outbox;

[SugarTable("outbox_messages")]
public sealed class OutboxMessage
{
    [SugarColumn(ColumnName = "event_id", IsPrimaryKey = true)]
    public Guid EventId { get; init; }

    [SugarColumn(ColumnName = "event_name", Length = 100)]
    public string EventName { get; init; } = string.Empty;

    [SugarColumn(ColumnName = "aggregate_id")]
    public long AggregateId { get; init; }

    [SugarColumn(ColumnName = "payload")]
    public string Payload { get; init; } = string.Empty;

    [SugarColumn(ColumnName = "occurred_at")]
    public DateTime OccurredAt { get; init; }

    [SugarColumn(ColumnName = "processed_at", IsNullable = true)]
    public DateTime? ProcessedAt { get; set; }

    [SugarColumn(ColumnName = "last_attempt_at", IsNullable = true)]
    public DateTime? LastAttemptAt { get; set; }

    [SugarColumn(ColumnName = "failure_count")]
    public int FailureCount { get; set; }

    [SugarColumn(ColumnName = "last_error", IsNullable = true, Length = 4000)]
    public string? LastError { get; set; }
}
