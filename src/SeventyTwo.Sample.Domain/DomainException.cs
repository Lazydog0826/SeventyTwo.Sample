namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 领域错误类型，用于由应用边界将领域错误转换为对应的协议响应。
/// </summary>
public enum DomainErrorType
{
    /// <summary>
    /// 输入未满足领域模型的格式、范围或不变量约束。
    /// </summary>
    Validation,

    /// <summary>
    /// 执行业务操作所需的领域资源不存在。
    /// </summary>
    NotFound,

    /// <summary>
    /// 操作与资源当前状态冲突，例如并发版本变化或唯一性冲突。
    /// </summary>
    Conflict,

    /// <summary>
    /// 输入本身有效，但当前业务规则不允许执行该操作。
    /// </summary>
    BusinessRule,
}

public class DomainException(string message, DomainErrorType errorType = DomainErrorType.BusinessRule)
    : Exception(message)
{
    public DomainErrorType ErrorType { get; } = errorType;
}
