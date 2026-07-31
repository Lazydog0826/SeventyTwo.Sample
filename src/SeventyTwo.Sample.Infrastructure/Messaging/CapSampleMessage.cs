namespace SeventyTwo.Sample.Infrastructure.Messaging;

public sealed record CapSampleMessage(Guid Id, string Content, DateTimeOffset PublishedAt)
{
    public const string TopicName = "seventytwo.sample.cap-sample";
}
