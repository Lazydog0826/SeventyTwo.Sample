namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户信息缓存失效消息。
/// </summary>
/// <param name="UserId">需要删除信息缓存的用户 ID。</param>
public sealed record UserInfoCacheInvalidationMessage(Guid UserId)
{
    public const string TopicName = "seventytwo.sample.user-info.cache.invalidate";

    public const string ConsumerGroup = "seventytwo.sample.user-info.cache.invalidate";
}

/// <summary>
/// 用户信息缓存失效消息发布器。
/// </summary>
public interface IUserInfoCacheInvalidationPublisher
{
    /// <summary>
    /// 发布指定用户的信息缓存失效消息。
    /// </summary>
    Task PublishAsync(Guid userId, CancellationToken cancellationToken);
}
