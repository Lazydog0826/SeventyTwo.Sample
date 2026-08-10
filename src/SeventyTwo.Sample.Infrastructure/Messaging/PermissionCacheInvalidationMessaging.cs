using DotNetCore.CAP;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Permissions;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

/// <summary>
/// 基于 CAP 的权限缓存失效消息发布器。
/// </summary>
[AutofacDependency(typeof(IPermissionCacheInvalidationPublisher))]
public sealed class CapPermissionCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IPermissionCacheInvalidationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(CancellationToken cancellationToken)
    {
        var message = new PermissionCacheInvalidationMessage(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        return capPublisher.PublishAsync(
            PermissionCacheInvalidationMessage.TopicName,
            message,
            cancellationToken: cancellationToken
        );
    }
}

/// <summary>
/// 权限缓存失效消息消费者。
/// </summary>
public sealed class PermissionCacheInvalidationConsumer(PermissionCacheService cacheService)
{
    /// <summary>
    /// 消费权限缓存失效消息并清除本地权限缓存。
    /// </summary>
    /// <param name="message">权限缓存失效消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [CapSubscribe(
        PermissionCacheInvalidationMessage.TopicName,
        Group = PermissionCacheInvalidationMessage.ConsumerGroup
    )]
    public Task ConsumeAsync(PermissionCacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        return cacheService.InvalidateAsync(cancellationToken);
    }
}

/// <summary>
/// 基于 CAP 的用户权限缓存失效消息发布器。
/// </summary>
[AutofacDependency(typeof(IUserPermissionCacheInvalidationPublisher))]
public sealed class CapUserPermissionCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IUserPermissionCacheInvalidationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(Guid userId, bool isSuperAdmin, CancellationToken cancellationToken)
    {
        var message = new UserPermissionCacheInvalidationMessage(userId, isSuperAdmin);
        return capPublisher.PublishAsync(
            UserPermissionCacheInvalidationMessage.TopicName,
            message,
            cancellationToken: cancellationToken
        );
    }
}

/// <summary>
/// 用户权限缓存失效消息消费者。
/// </summary>
public sealed class UserPermissionCacheInvalidationConsumer(IUserPermissionCacheService userPermissionCacheService)
{
    /// <summary>
    /// 消费用户权限缓存失效消息并删除对应缓存。
    /// </summary>
    [CapSubscribe(
        UserPermissionCacheInvalidationMessage.TopicName,
        Group = UserPermissionCacheInvalidationMessage.ConsumerGroup
    )]
    public Task ConsumeAsync(UserPermissionCacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        return message.IsSuperAdmin
            ? userPermissionCacheService.DeleteSuperAdminAsync(cancellationToken)
            : userPermissionCacheService.DeleteAsync(message.UserId, cancellationToken);
    }
}
