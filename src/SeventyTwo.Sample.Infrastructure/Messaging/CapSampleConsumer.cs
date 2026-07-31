using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace SeventyTwo.Sample.Infrastructure.Messaging;

public sealed class CapSampleConsumer(ILogger<CapSampleConsumer> logger) : ICapSubscribe
{
    [CapSubscribe(CapSampleMessage.TopicName)]
    public Task ConsumeAsync(CapSampleMessage message)
    {
        logger.LogInformation(
            "CAP 示例消息消费成功，消息标识：{MessageId}，内容：{Content}，发布时间：{PublishedAt}",
            message.Id,
            message.Content,
            message.PublishedAt
        );
        return Task.CompletedTask;
    }
}
