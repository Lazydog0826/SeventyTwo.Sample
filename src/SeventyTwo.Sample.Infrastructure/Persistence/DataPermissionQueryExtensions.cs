using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Organizations;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

/// <summary>
/// 数据权限查询扩展。
/// 在数据库访问层面按当前用户的数据权限类型为查询追加过滤条件；
/// 约束为 <see cref="IDataPermissionScoped"/>，即实体必须携带数据权限过滤所需的机构归属与创建人。
/// </summary>
public static class DataPermissionQueryExtensions
{
    /// <summary>
    /// 按数据权限范围过滤查询：
    /// 全部数据不过滤；本机构按机构 ID 匹配；
    /// 本机构与下级机构通过 organization 表 Path 前缀子查询在数据库层面匹配；
    /// 自己的数据按创建人匹配。
    /// 类型为未定义值（如 0）时视为系统内部调用，不过滤。
    /// </summary>
    /// <typeparam name="T">持久化实体类型。</typeparam>
    /// <param name="query">待过滤的查询。</param>
    /// <param name="dataPermissionScope">当前用户的数据权限范围。</param>
    /// <returns>追加了数据权限条件的查询。</returns>
    public static ISugarQueryable<T> ApplyDataPermission<T>(
        this ISugarQueryable<T> query,
        DataPermissionScope dataPermissionScope
    )
        where T : class, IDataPermissionScoped
    {
        // SqlSugar 的 Where 会原地修改查询对象；Clone 保证本扩展无副作用，
        // 允许调用方把同一条基础查询叠加到多个不同权限的查询上。
        query = query.Clone();
        return dataPermissionScope.DataPermissionType switch
        {
            DataPermissionType.Organization => ApplyOrganization(query, dataPermissionScope.OrgId),
            DataPermissionType.OrganizationAndDescendants => ApplyOrganizationAndDescendants(
                query,
                dataPermissionScope.OrgId,
                dataPermissionScope.OrganizationPath
            ),
            DataPermissionType.Self => ApplySelf(query, dataPermissionScope.UserId),
            _ => query,
        };
    }

    /// <summary>
    /// 本机构数据：仅保留机构 ID 与当前用户一致的行。
    /// </summary>
    private static ISugarQueryable<T> ApplyOrganization<T>(ISugarQueryable<T> query, Guid orgId)
        where T : class, IDataPermissionScoped
    {
        return query.Where(x => x.OrgId == orgId);
    }

    /// <summary>
    /// 本机构与下级机构数据：行的机构 ID 命中本机构，
    /// 或其机构 Path 以本机构 Path 为字符串前缀（含本机构自身与下级机构）。
    /// </summary>
    private static ISugarQueryable<T> ApplyOrganizationAndDescendants<T>(
        ISugarQueryable<T> query,
        Guid orgId,
        string? organizationPath
    )
        where T : class, IDataPermissionScoped
    {
        // SqlSugar 不支持把子查询结果作为 StartsWith 参数，本机构 Path 必须由调用方传入；
        // Path 段均为定长 GUID 字符串，字符串前缀命中即本机构或其下级机构。
        if (string.IsNullOrWhiteSpace(organizationPath))
        {
            throw new InvalidOperationException(
                $"数据权限类型为 {nameof(DataPermissionType.OrganizationAndDescendants)} 时必须提供机构路径。"
            );
        }

        // 使用 EXISTS 关联子查询而非 LeftJoin：Join 会让查询进入多表形态，
        // SqlSugar 要求后续 OrderBy/Where 使用与 Join 相同的别名，调用方链式调用会因别名不一致抛异常；
        // 子查询保持单表形态，调用方可继续按单表约定叠加条件与排序。
        // 行的机构缺失时子查询不命中，仍可走 "机构 ID 等于本机构" 分支，与原左联语义一致；
        // 机构是否软删不属于数据权限范畴，可见性由基础查询自行控制。
        return query.Where(x =>
            x.OrgId == orgId
            || SqlFunc
                .Subqueryable<OrganizationRecord>()
                .Where(org => org.Id == x.OrgId && org.Path.StartsWith(organizationPath))
                .Any()
        );
    }

    /// <summary>
    /// 自己的数据：仅保留当前用户创建的行。
    /// </summary>
    private static ISugarQueryable<T> ApplySelf<T>(ISugarQueryable<T> query, Guid userId)
        where T : class, IDataPermissionScoped
    {
        return query.Where(x => x.CreatedBy == userId);
    }
}
