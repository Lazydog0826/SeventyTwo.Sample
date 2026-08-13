namespace SeventyTwo.Sample.Application.DataDictionaries;

/// <summary>字典选项缓存失效消息。</summary>
/// <param name="Codes">需要清理缓存的字典编码。</param>
public sealed record DataDictionaryCacheInvalidationMessage(string[] Codes)
{
    public const string TopicName = "seventytwo.sample.data-dictionaries.cache.invalidate";

    public const string ConsumerGroup = "seventytwo.sample.data-dictionaries.cache.invalidate";
}

/// <summary>字典选项缓存失效消息发布器。</summary>
public interface IDataDictionaryCacheInvalidationPublisher
{
    /// <summary>发布指定编码的缓存失效消息。</summary>
    Task PublishAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}
