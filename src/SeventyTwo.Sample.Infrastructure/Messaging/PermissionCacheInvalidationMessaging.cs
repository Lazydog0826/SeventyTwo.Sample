using DotNetCore.CAP;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Permissions;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

[AutofacDependency(typeof(IPermissionCacheInvalidationPublisher))]
public sealed class CapPermissionCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IPermissionCacheInvalidationPublisher
{
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

public sealed class PermissionCacheInvalidationConsumer(PermissionMemoryCacheService memoryCacheService)
{
    [CapSubscribe(
        PermissionCacheInvalidationMessage.TopicName,
        Group = PermissionCacheInvalidationMessage.ConsumerGroup
    )]
    public Task ConsumeAsync(
        PermissionCacheInvalidationMessage message,
        CancellationToken cancellationToken
    )
    {
        return memoryCacheService.InvalidateAsync(cancellationToken);
    }
}

