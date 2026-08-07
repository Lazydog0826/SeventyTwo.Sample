// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 聚合根公共字段基类。
/// </summary>
public abstract class AggregateRoot
{
    /// <summary>
    /// 主键 UUID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// 删除人 UUIDv7。
    /// </summary>
    public Guid? DeleteBy { get; set; }

    /// <summary>
    /// 删除时间。
    /// </summary>
    public DateTimeOffset? DeleteAt { get; set; }

    /// <summary>
    /// 创建人 UUIDv7。
    /// </summary>
    public Guid CreatedBy { get; set; } = SystemIds.System;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 修改人 UUIDv7。
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 修改时间。
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 组织 UUIDv7。
    /// </summary>
    public Guid OrgId { get; set; } = Guid.Empty;

    /// <summary>
    /// 乐观锁版本 UUIDv7。
    /// </summary>
    public Guid Version { get; set; } = Guid.CreateVersion7();
}
