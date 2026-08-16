using SeventyTwo.Sample.Domain;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

/// <summary>
/// 持久化实体公共字段基类。
/// </summary>
/// <remarks>
/// 基类不预置公共字段默认值：主键与乐观锁版本由构造处显式提供或经聚合映射带入，
/// 创建时间、创建人、机构归属在未指定时由公共字段拦截器在插入时补全（需实现对应接口）。
/// </remarks>
public abstract class BaseEntity : IDataPermissionScoped
{
    /// <summary>
    /// 主键 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "id", ColumnDescription = "主键ID", IsPrimaryKey = true, ColumnDataType = "uuid")]
    public Guid Id { get; init; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    [SugarColumn(ColumnName = "enable", ColumnDescription = "是否启用")]
    public bool Enable { get; init; }

    /// <summary>
    /// 删除人 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "delete_by", ColumnDescription = "删除人", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? DeleteBy { get; init; }

    /// <summary>
    /// 删除时间。
    /// </summary>
    [SugarColumn(ColumnName = "delete_at", ColumnDescription = "删除时间", IsNullable = true)]
    public DateTimeOffset? DeleteAt { get; init; }

    /// <summary>
    /// 创建人 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "created_by", ColumnDescription = "创建人", ColumnDataType = "uuid")]
    public Guid CreatedBy { get; init; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    [SugarColumn(ColumnName = "created_at", ColumnDescription = "创建时间")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 修改人 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "updated_by", ColumnDescription = "修改人", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? UpdatedBy { get; init; }

    /// <summary>
    /// 修改时间。
    /// </summary>
    [SugarColumn(ColumnName = "updated_at", ColumnDescription = "修改时间", IsNullable = true)]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// 组织 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "org_id", ColumnDescription = "机构ID", ColumnDataType = "uuid")]
    public Guid OrgId { get; init; }

    /// <summary>
    /// 乐观锁版本 UUIDv7。
    /// </summary>
    [SugarColumn(ColumnName = "version", ColumnDescription = "并发更新控制", ColumnDataType = "uuid")]
    public Guid Version { get; init; }

    /// <summary>
    /// 将持久化实体的公共字段赋值给聚合根。
    /// </summary>
    /// <param name="aggregateRoot">待赋值的聚合根。</param>
    public void AggregateRootToEntity(AggregateRoot aggregateRoot)
    {
        aggregateRoot.Id = Id;
        aggregateRoot.Enable = Enable;
        aggregateRoot.DeleteBy = DeleteBy;
        aggregateRoot.DeleteAt = DeleteAt;
        aggregateRoot.CreatedBy = CreatedBy;
        aggregateRoot.CreatedAt = CreatedAt;
        aggregateRoot.UpdatedBy = UpdatedBy;
        aggregateRoot.UpdatedAt = UpdatedAt;
        aggregateRoot.OrgId = OrgId;
        aggregateRoot.Version = Version;
    }
}
