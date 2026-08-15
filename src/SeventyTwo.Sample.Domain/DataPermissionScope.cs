using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Domain;

/// <summary>
/// 当前用户的数据权限范围。
/// 由 WebApi 层从业务 JWT claims 构建，应用层按需补充机构路径后传入仓储，
/// 供数据库访问层按数据权限过滤查询。
/// </summary>
/// <param name="DataPermissionType">数据权限类型。</param>
/// <param name="UserId">用户 ID。</param>
/// <param name="OrgId">用户所属机构 ID。</param>
/// <param name="OrganizationPath">用户所属机构的层级路径；本机构与下级机构类型过滤时使用。</param>
public sealed record DataPermissionScope(
    DataPermissionType DataPermissionType,
    Guid UserId,
    Guid OrgId,
    string? OrganizationPath = null
);
