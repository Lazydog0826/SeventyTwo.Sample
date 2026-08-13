using SeventyTwo.Sample.Domain.Permissions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>权限管理列表项。</summary>
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

/// <summary>创建权限输入。</summary>
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

/// <summary>修改权限输入；<paramref name="Version"/> 用于乐观并发控制。</summary>
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

/// <summary>用户权限输出。</summary>
public sealed record PermissionOutput(IReadOnlyList<PermissionMenuOutput> Menus, IReadOnlyList<string> ButtonCodes);

/// <summary>用户授权编辑数据。</summary>
public sealed record UserAuthorizationOutput(
    IReadOnlyList<PermissionListOutput> Permissions,
    IReadOnlyList<Guid> PermissionIds
);

/// <summary>用户默认页面候选项。</summary>
public sealed record DefaultPageOptionOutput(Guid Id, string Title, int SortOrder);

/// <summary>目录或页面权限。</summary>
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
