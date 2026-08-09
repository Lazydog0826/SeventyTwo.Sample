using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Permissions;

[SugarTable("permission")]
[SugarIndex("uq_permission_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_permission_parent_id", nameof(ParentId), OrderByType.Asc)]
internal sealed class PermissionRecord : BaseEntity
{
    /// <summary>
    /// 权限编码。
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 权限标题。
    /// </summary>
    [SugarColumn(ColumnName = "title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 权限类型。
    /// </summary>
    [SugarColumn(ColumnName = "type")]
    public PermissionType Type { get; init; }

    /// <summary>
    /// 排序号。
    /// </summary>
    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; init; }

    /// <summary>
    /// 图标。
    /// </summary>
    [SugarColumn(ColumnName = "icon")]
    public string Icon { get; init; } = string.Empty;

    /// <summary>
    /// Vue 组件路径。
    /// </summary>
    [SugarColumn(ColumnName = "vue_component_path")]
    public string VueComponentPath { get; init; } = string.Empty;

    /// <summary>
    /// 路由路径。
    /// </summary>
    [SugarColumn(ColumnName = "route_path")]
    public string RoutePath { get; init; } = string.Empty;

    /// <summary>
    /// 路由名称。
    /// </summary>
    [SugarColumn(ColumnName = "route_name")]
    public string RouteName { get; init; } = string.Empty;

    /// <summary>
    /// 上级权限 ID。
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 路由元数据。
    /// </summary>
    [SugarColumn(ColumnName = "meta_data", ColumnDataType = "jsonb", IsJson = true)]
    public PermissionMetaData MetaData { get; init; }
}

[SugarTable("user_permission")]
[SugarIndex(
    "uq_user_permission_user_permission",
    nameof(UserId),
    OrderByType.Asc,
    nameof(PermissionId),
    OrderByType.Asc,
    true
)]
[SugarIndex("ix_user_permission_user_id", nameof(UserId), OrderByType.Asc)]
[SugarIndex("ix_user_permission_permission_id", nameof(PermissionId), OrderByType.Asc)]
internal sealed class UserPermissionRecord : BaseEntity
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    [SugarColumn(ColumnName = "user_id", ColumnDataType = "uuid")]
    public Guid UserId { get; init; }

    /// <summary>
    /// 权限 ID。
    /// </summary>
    [SugarColumn(ColumnName = "permission_id", ColumnDataType = "uuid")]
    public Guid PermissionId { get; init; }
}
