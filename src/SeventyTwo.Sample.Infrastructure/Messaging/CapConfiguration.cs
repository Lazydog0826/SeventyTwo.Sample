namespace SeventyTwo.Sample.Infrastructure.Messaging;

public sealed class CapConfiguration
{
    /// <summary>
    /// 获取 RabbitMQ 主机名。
    /// </summary>
    public string RabbitMqHostName { get; init; } = string.Empty;

    /// <summary>
    /// 获取 RabbitMQ 用户名。
    /// </summary>
    public string RabbitMqUserName { get; init; } = string.Empty;

    /// <summary>
    /// 获取 RabbitMQ 密码。
    /// </summary>
    public string RabbitMqPassword { get; init; } = string.Empty;

    /// <summary>
    /// 获取 RabbitMQ 虚拟主机。
    /// </summary>
    public string RabbitMqVirtualHost { get; init; } = string.Empty;
}
