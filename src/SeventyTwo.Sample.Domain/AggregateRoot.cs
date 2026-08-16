// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 聚合根公共字段基类。
/// </summary>
/// <remarks>
/// 基类不预置公共字段默认值：Enable 与 Version 由聚合创建构造函数显式初始化；
/// 创建时间由业务按需指定业务发生时间，未指定时由持久化层在插入时生成；
/// 从持久化还原时公共字段以记录值为准。
/// </remarks>
public abstract class AggregateRoot : IDataPermissionScoped
{
    /// <summary>
    /// 主键 UUID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enable { get; set; }

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
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

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
    public Guid OrgId { get; set; }

    /// <summary>
    /// 乐观锁版本 UUIDv7。
    /// </summary>
    public Guid Version { get; set; }
}
