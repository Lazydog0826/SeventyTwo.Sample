namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限匹配模式。
/// </summary>
public enum PermissionMatchMode
{
    /// <summary>
    /// 满足任意一个权限即可。
    /// </summary>
    Any,

    /// <summary>
    /// 必须满足全部权限。
    /// </summary>
    All,
}

/// <summary>
/// 用户权限检查器。
/// </summary>
public interface IUserPermissionChecker
{
    /// <summary>
    /// 获取用户权限编码。
    /// </summary>
    Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 判断用户是否拥有指定权限。
    /// </summary>
    Task<bool> HasAsync(
        Guid userId,
        IReadOnlyCollection<string> permissionCodes,
        PermissionMatchMode matchMode,
        CancellationToken cancellationToken
    );
}
