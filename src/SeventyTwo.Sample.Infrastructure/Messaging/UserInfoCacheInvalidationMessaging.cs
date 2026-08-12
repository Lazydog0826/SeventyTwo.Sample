using DotNetCore.CAP;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Users;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

/// <summary>
/// 基于 CAP 的用户信息缓存失效消息发布器。
/// </summary>
[AutofacDependency(typeof(IUserInfoCacheInvalidationPublisher))]
public sealed class CapUserInfoCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IUserInfoCacheInvalidationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(Guid userId, CancellationToken cancellationToken)
    {
        var message = new UserInfoCacheInvalidationMessage(userId);
        return capPublisher.PublishAsync(
            UserInfoCacheInvalidationMessage.TopicName,
            message,
            cancellationToken: cancellationToken
        );
    }
}

/// <summary>
/// 用户信息缓存失效消息消费者。
/// </summary>
public sealed class UserInfoCacheInvalidationConsumer(UserInfoCacheService userInfoCacheService) : ICapSubscribe
{
    /// <summary>
    /// 消费消息并删除指定用户的信息缓存。
    /// </summary>
    [CapSubscribe(UserInfoCacheInvalidationMessage.TopicName, Group = UserInfoCacheInvalidationMessage.ConsumerGroup)]
    public Task ConsumeAsync(UserInfoCacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        return userInfoCacheService.DeleteCacheAsync(message.UserId, cancellationToken);
    }
}
