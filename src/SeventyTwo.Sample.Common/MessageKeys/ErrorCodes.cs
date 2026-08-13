namespace SeventyTwo.Sample.Common.MessageKeys;

/// <summary>
/// API 错误分类键，供客户端执行稳定的流程判断。
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// 认证失败。
    /// </summary>
    public const string Authentication = "error.authentication";

    /// <summary>
    /// 请求参数校验失败。
    /// </summary>
    public const string Validation = "error.validation";

    /// <summary>
    /// 领域业务规则校验失败。
    /// </summary>
    public const string Domain = "error.domain";

    /// <summary>
    /// 系统内部错误。
    /// </summary>
    public const string Internal = "error.internal";
}
