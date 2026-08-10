namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 用户权限缓存失效消息。
/// </summary>
/// <param name="UserId">用户 ID；超级管理员共享缓存使用 <see cref="Guid.Empty" />。</param>
/// <param name="IsSuperAdmin">是否删除超级管理员共享权限缓存。</param>
public sealed record UserPermissionCacheInvalidationMessage(Guid UserId, bool IsSuperAdmin)
{
    public const string TopicName = "seventytwo.sample.user-permissions.cache.invalidate";

    public const string ConsumerGroup = "seventytwo.sample.user-permissions.cache.invalidate";
}

/// <summary>
/// 用户权限缓存失效消息发布器。
/// </summary>
public interface IUserPermissionCacheInvalidationPublisher
{
    /// <summary>
    /// 发布用户权限缓存失效消息。
    /// </summary>
    Task PublishAsync(Guid userId, bool isSuperAdmin, CancellationToken cancellationToken);
}
