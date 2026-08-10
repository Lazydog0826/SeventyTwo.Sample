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
public sealed class PermissionCacheInvalidationConsumer(PermissionMemoryCacheService memoryCacheService)
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
        return memoryCacheService.InvalidateAsync(cancellationToken);
    }
}
