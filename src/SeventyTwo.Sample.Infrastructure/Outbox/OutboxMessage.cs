using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Outbox;

[SugarTable("outbox_messages")]
public sealed class OutboxMessage
{
    /// <summary>
    /// 事件雪花标识。
    /// </summary>
    [SugarColumn(ColumnName = "event_id", IsPrimaryKey = true)]
    public long EventId { get; init; } = Yitter.IdGenerator.YitIdHelper.NextId();

    /// <summary>
    /// 事件名称。
    /// </summary>
    [SugarColumn(ColumnName = "event_name", Length = 100)]
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// 聚合根标识。
    /// </summary>
    [SugarColumn(ColumnName = "aggregate_id")]
    public long AggregateId { get; init; }

    /// <summary>
    /// 事件消息内容。
    /// </summary>
    [SugarColumn(ColumnName = "payload")]
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// 事件发生时间。
    /// </summary>
    [SugarColumn(ColumnName = "occurred_at")]
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// 处理完成时间，未处理时为空。
    /// </summary>
    [SugarColumn(ColumnName = "processed_at", IsNullable = true)]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// 最近一次处理尝试时间。
    /// </summary>
    [SugarColumn(ColumnName = "last_attempt_at", IsNullable = true)]
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// 处理失败次数。
    /// </summary>
    [SugarColumn(ColumnName = "failure_count")]
    public int FailureCount { get; set; }

    /// <summary>
    /// 最近一次处理失败信息。
    /// </summary>
    [SugarColumn(ColumnName = "last_error", IsNullable = true, Length = 4000)]
    public string? LastError { get; set; }
}
