// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限变更后发布的全量权限缓存失效消息。
/// </summary>
public sealed record PermissionCacheInvalidationMessage(Guid Id, DateTimeOffset PublishedAt)
{
    public const string TopicName = "seventytwo.sample.permissions.cache.invalidate";

    public const string ConsumerGroup = "seventytwo.sample.permissions.cache.invalidate";
}

/// <summary>
/// 权限缓存失效消息发布器。
/// </summary>
public interface IPermissionCacheInvalidationPublisher
{
    Task PublishAsync(CancellationToken cancellationToken);
}
