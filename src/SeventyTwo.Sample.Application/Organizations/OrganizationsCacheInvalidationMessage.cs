namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>
/// 机构路径缓存失效消息。
/// </summary>
/// <param name="OrganizationIds">需要删除路径缓存的机构 ID；Path 级联变更时包含全部后代机构。</param>
public sealed record OrganizationsCacheInvalidationMessage(Guid[] OrganizationIds)
{
    public const string TopicName = "seventytwo.sample.organizations.cache.invalidate";

    public const string ConsumerGroup = "seventytwo.sample.organizations.cache.invalidate";
}

/// <summary>
/// 机构路径缓存失效消息发布器。
/// </summary>
public interface IOrganizationsCacheInvalidationPublisher
{
    /// <summary>
    /// 发布指定机构的路径缓存失效消息。
    /// </summary>
    Task PublishAsync(IReadOnlyCollection<Guid> organizationIds, CancellationToken cancellationToken);
}
