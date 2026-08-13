using DotNetCore.CAP;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.DataDictionaries;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

/// <summary>基于 CAP 的字典选项缓存失效消息发布器。</summary>
[AutofacDependency(typeof(IDataDictionaryCacheInvalidationPublisher))]
public sealed class CapDataDictionaryCacheInvalidationPublisher(ICapPublisher capPublisher)
    : IDataDictionaryCacheInvalidationPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var message = new DataDictionaryCacheInvalidationMessage([.. codes.Distinct(StringComparer.Ordinal)]);
        return capPublisher.PublishAsync(
            DataDictionaryCacheInvalidationMessage.TopicName,
            message,
            cancellationToken: cancellationToken
        );
    }
}

/// <summary>字典选项缓存失效消息消费者。</summary>
public sealed class DataDictionaryCacheInvalidationConsumer(DataDictionaryCacheService cacheService) : ICapSubscribe
{
    /// <summary>消费消息并删除指定编码的缓存。</summary>
    [CapSubscribe(
        DataDictionaryCacheInvalidationMessage.TopicName,
        Group = DataDictionaryCacheInvalidationMessage.ConsumerGroup
    )]
    public async Task ConsumeAsync(DataDictionaryCacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        foreach (var code in message.Codes.Distinct(StringComparer.Ordinal))
        {
            await cacheService.DeleteAsync(code, cancellationToken);
        }
    }
}
