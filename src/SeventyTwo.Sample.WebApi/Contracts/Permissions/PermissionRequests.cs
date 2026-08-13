using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.WebApi.Contracts.Permissions;

/// <summary>
/// 权限创建请求。
/// </summary>
/// <param name="Code">权限编码。</param>
/// <param name="Title">权限标题。</param>
/// <param name="Type">权限类型。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="Icon">图标。</param>
/// <param name="VueComponentPath">Vue 组件路径。</param>
/// <param name="RoutePath">路由路径。</param>
/// <param name="RouteName">路由名称。</param>
/// <param name="ParentId">上级权限 ID。</param>
/// <param name="MetaData">路由元数据。</param>
public record CreatePermissionRequest(
    string Code,
    string Title,
    PermissionType Type,
    bool Enable,
    int SortOrder,
    string? Icon,
    string? VueComponentPath,
    string? RoutePath,
    string? RouteName,
    Guid? ParentId,
    PermissionMetaData? MetaData
);

/// <summary>
/// 权限修改请求。
/// </summary>
/// <param name="Id">权限 ID。</param>
/// <param name="Code">权限编码。</param>
/// <param name="Title">权限标题。</param>
/// <param name="Type">权限类型。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="Icon">图标。</param>
/// <param name="VueComponentPath">Vue 组件路径。</param>
/// <param name="RoutePath">路由路径。</param>
/// <param name="RouteName">路由名称。</param>
/// <param name="ParentId">上级权限 ID。</param>
/// <param name="MetaData">路由元数据。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record UpdatePermissionRequest(
    Guid Id,
    string Code,
    string Title,
    PermissionType Type,
    bool Enable,
    int SortOrder,
    string? Icon,
    string? VueComponentPath,
    string? RoutePath,
    string? RouteName,
    Guid? ParentId,
    PermissionMetaData? MetaData,
    Guid Version
)
    : CreatePermissionRequest(
        Code,
        Title,
        Type,
        Enable,
        SortOrder,
        Icon,
        VueComponentPath,
        RoutePath,
        RouteName,
        ParentId,
        MetaData
    );

/// <summary>
/// 权限删除请求。
/// </summary>
/// <param name="Id">权限 ID。</param>
public sealed record DeletePermissionRequest(Guid Id);
