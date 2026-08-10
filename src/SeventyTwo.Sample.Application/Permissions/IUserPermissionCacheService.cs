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
/// 用户权限缓存服务。
/// </summary>
public interface IUserPermissionCacheService
{
    /// <summary>
    /// 获取用户权限编码。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户当前拥有的有效权限编码。</returns>
    Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 删除用户权限缓存。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// 删除超级管理员权限缓存。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteSuperAdminAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 判断用户是否拥有指定权限。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="permissionCodes">待匹配的权限编码。</param>
    /// <param name="matchMode">权限匹配模式。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>满足匹配条件时返回 <see langword="true"/>。</returns>
    Task<bool> HasAsync(
        Guid userId,
        IReadOnlyCollection<string> permissionCodes,
        PermissionMatchMode matchMode,
        CancellationToken cancellationToken
    );
}
