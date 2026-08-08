namespace SeventyTwo.Sample.Domain.Permissions;

/// <summary>
/// 权限仓储。
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// 获取所有有效权限。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有有效权限。</returns>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取用户拥有的权限编码。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户拥有的权限编码。</returns>
    Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
