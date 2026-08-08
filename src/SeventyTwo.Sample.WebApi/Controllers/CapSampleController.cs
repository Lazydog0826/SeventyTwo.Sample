using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Infrastructure.Messaging;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// CAP 消息示例接口。
/// </summary>
/// <param name="capPublisher">CAP 消息发布器。</param>
[ApiController]
[Route("api/cap-sample")]
public sealed class CapSampleController(ICapPublisher capPublisher) : ControllerBase
{
    /// <summary>
    /// 发布 CAP 示例消息。
    /// </summary>
    /// <param name="request">消息发布请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已发布的消息。</returns>
    [HttpPost("publish")]
    public async Task<CapSampleMessage> Publish(CapSamplePublishRequest request, CancellationToken cancellationToken)
    {
        var message = new CapSampleMessage(Guid.NewGuid(), request.Content, DateTimeOffset.UtcNow);
        await capPublisher.PublishAsync(CapSampleMessage.TopicName, message, cancellationToken: cancellationToken);
        return message;
    }
}

/// <summary>
/// CAP 示例消息发布请求。
/// </summary>
/// <param name="Content">消息内容。</param>
public sealed record CapSamplePublishRequest(string Content);
