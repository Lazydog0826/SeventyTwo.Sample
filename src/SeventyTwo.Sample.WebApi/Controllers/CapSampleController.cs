using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Infrastructure.Messaging;

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/cap-sample")]
public sealed class CapSampleController(ICapPublisher capPublisher) : ControllerBase
{
    [HttpPost("publish")]
    public async Task<CapSampleMessage> Publish(CapSamplePublishRequest request, CancellationToken cancellationToken)
    {
        var message = new CapSampleMessage(Guid.NewGuid(), request.Content, DateTimeOffset.UtcNow);
        await capPublisher.PublishAsync(CapSampleMessage.TopicName, message, cancellationToken: cancellationToken);
        return message;
    }
}

public sealed record CapSamplePublishRequest(string Content);
