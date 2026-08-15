namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 机构归属实体规范。
/// 实现该接口的持久化实体在插入时由公共字段拦截器补全当前用户所属机构，
/// 机构仍为 <see cref="Guid.Empty" /> 时才补全，保留调用方显式指定的归属；
/// 机构归属恒为空的全局数据实体不应实现本接口。
/// </summary>
public interface IOrgScoped
{
    /// <summary>
    /// 组织 UUIDv7。
    /// </summary>
    Guid OrgId { get; }
}
