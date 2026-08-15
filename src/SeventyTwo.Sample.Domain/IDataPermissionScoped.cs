namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 数据权限范围实体规范。
/// 实现该接口的实体纳入数据权限过滤范围，必须携带数据权限过滤所需的机构归属与创建人，
/// 供 <c>DataPermissionQueryExtensions</c> 在数据库访问层面按数据权限过滤数据。
/// </summary>
public interface IDataPermissionScoped
{
    /// <summary>
    /// 组织 UUIDv7。
    /// </summary>
    Guid OrgId { get; }

    /// <summary>
    /// 创建人 UUIDv7。
    /// </summary>
    Guid CreatedBy { get; }
}
