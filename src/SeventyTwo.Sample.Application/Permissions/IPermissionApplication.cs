using SeventyTwo.Sample.Domain.Permissions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限应用服务。
/// </summary>
public interface IPermissionApplication
{
    /// <summary>
    /// 获取指定权限的编辑详情。
    /// </summary>
    Task<PermissionListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 创建权限并发布权限列表缓存失效消息。
    /// </summary>
    Task<PermissionListOutput> CreateAsync(CreatePermissionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 修改权限并发布权限列表缓存失效消息。
    /// </summary>
    Task UpdateAsync(Guid id, UpdatePermissionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 物理删除权限及用户权限关联，并发布权限列表缓存失效消息。
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取权限管理列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除权限。</returns>
    Task<IReadOnlyList<PermissionListOutput>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户权限。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户权限。</returns>
    Task<PermissionOutput> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户授权编辑数据，包含全部权限及用户当前关联的权限 ID。
    /// </summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户授权编辑数据。</returns>
    Task<UserAuthorizationOutput> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 整体保存用户授权，允许保留已禁用权限的关联。
    /// </summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="permissionIds">完整权限 ID 集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AuthorizeAsync(Guid userId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken);
}

/// <summary>
/// 权限管理列表项。
/// </summary>
public sealed record PermissionListOutput
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public PermissionType Type { get; init; }

    public bool Enable { get; init; }

    public int SortOrder { get; init; }

    public string Icon { get; init; } = string.Empty;

    public string VueComponentPath { get; init; } = string.Empty;

    public string RoutePath { get; init; } = string.Empty;

    public string RouteName { get; init; } = string.Empty;

    public PermissionMetaData MetaData { get; init; }

    public Guid? ParentId { get; init; }

    public Guid Version { get; init; }
}

/// <summary>
/// 创建权限输入。
/// </summary>
public sealed record CreatePermissionInput(
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
/// 修改权限输入；<paramref name="Version"/> 用于乐观并发控制。
/// </summary>
public sealed record UpdatePermissionInput(
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
);

/// <summary>
/// 用户权限输出。
/// </summary>
/// <param name="Menus">目录和页面权限列表。</param>
/// <param name="ButtonCodes">按钮权限编码集合。</param>
public sealed record PermissionOutput(IReadOnlyList<PermissionMenuOutput> Menus, IReadOnlyList<string> ButtonCodes);

/// <summary>
/// 用户授权编辑数据。
/// </summary>
/// <param name="Permissions">全部未删除权限。</param>
/// <param name="PermissionIds">用户当前关联的权限 ID，包含已禁用权限。</param>
public sealed record UserAuthorizationOutput(
    IReadOnlyList<PermissionListOutput> Permissions,
    IReadOnlyList<Guid> PermissionIds
);

/// <summary>
/// 目录或页面权限。
/// </summary>
public sealed record PermissionMenuOutput
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public PermissionType Type { get; init; }

    public int SortOrder { get; init; }

    public string Icon { get; init; } = string.Empty;

    public string VueComponentPath { get; init; } = string.Empty;

    public string RoutePath { get; init; } = string.Empty;

    public string RouteName { get; init; } = string.Empty;

    public PermissionMetaData MetaData { get; init; }

    public Guid? ParentId { get; init; }
}
