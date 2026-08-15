namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 审计字段实体规范。
/// 实现该接口的持久化实体由公共字段拦截器自动维护审计字段：
/// 插入时生成创建时间（覆盖预置值）并补全创建人（仍为系统默认值时才补全，保留显式赋值），
/// 实体更新时覆盖修改人与修改时间，取值来自当前业务用户上下文；
/// 映射与仓储不应再显式赋值这些字段，保持拦截器为唯一出口。
/// </summary>
public interface IAudited
{
    /// <summary>
    /// 创建人 UUIDv7。
    /// </summary>
    Guid CreatedBy { get; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 修改人 UUIDv7。
    /// </summary>
    Guid? UpdatedBy { get; }

    /// <summary>
    /// 修改时间。
    /// </summary>
    DateTimeOffset? UpdatedAt { get; }
}
