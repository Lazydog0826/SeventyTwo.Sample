using DotNetCore.CAP;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Organizations;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

/// <summary>
/// 基于 CAP 的机构路径缓存失效消息发布器。
/// </summary>
[AutofacDependency(typeof(IOrganizationsCacheInvalidationPublisher))]
public sealed class CapOrganizationsCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IOrganizationsCacheInvalidationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(IReadOnlyCollection<Guid> organizationIds, CancellationToken cancellationToken)
    {
        var message = new OrganizationsCacheInvalidationMessage([.. organizationIds]);
        return capPublisher.PublishAsync(
            OrganizationsCacheInvalidationMessage.TopicName,
            message,
            cancellationToken: cancellationToken
        );
    }
}

/// <summary>
/// 机构路径缓存失效消息消费者。
/// </summary>
public sealed class OrganizationsCacheInvalidationConsumer(OrganizationsCacheService organizationsCacheService)
    : ICapSubscribe
{
    /// <summary>
    /// 消费消息并删除指定机构的路径缓存。
    /// </summary>
    [CapSubscribe(
        OrganizationsCacheInvalidationMessage.TopicName,
        Group = OrganizationsCacheInvalidationMessage.ConsumerGroup
    )]
    public async Task ConsumeAsync(OrganizationsCacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        foreach (var organizationId in message.OrganizationIds)
        {
            await organizationsCacheService.DeleteCacheAsync(organizationId, cancellationToken);
        }
    }
}
