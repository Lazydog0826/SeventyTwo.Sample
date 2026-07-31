namespace SeventyTwo.Sample.Infrastructure.Messaging;

public sealed class CapConfiguration
{
    public string PostgreSqlConnectionString { get; init; } = string.Empty;

    public string RabbitMqHostName { get; init; } = string.Empty;

    public string RabbitMqUserName { get; init; } = string.Empty;

    public string RabbitMqPassword { get; init; } = string.Empty;

    public string RabbitMqVirtualHost { get; init; } = string.Empty;
}
