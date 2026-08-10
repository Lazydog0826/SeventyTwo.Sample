namespace SeventyTwo.Sample.WebApi.Infrastructure;

/// <summary>
/// 表示 API 请求参数未通过业务校验时发生的异常。
/// </summary>
/// <param name="message">校验失败的错误消息。</param>
public sealed class ApiValidationException(string message) : Exception(message);
