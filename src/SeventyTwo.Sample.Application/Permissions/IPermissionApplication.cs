using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限应用服务。
/// </summary>
public interface IPermissionApplication
{
    /// <summary>
    /// 获取用户权限。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户权限。</returns>
    Task<PermissionOutput> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// 用户权限输出。
/// </summary>
/// <param name="Menus">目录和页面权限列表。</param>
/// <param name="ButtonCodes">按钮权限编码集合。</param>
public sealed record PermissionOutput(
    IReadOnlyList<PermissionMenuOutput> Menus,
    IReadOnlyList<string> ButtonCodes
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

    public string? Icon { get; init; }

    public string? VueComponentPath { get; init; }

    public string? RoutePath { get; init; }

    public string? RouteName { get; init; }

    public PermissionMetaData MetaData { get; init; }

    public Guid? ParentId { get; init; }
}
