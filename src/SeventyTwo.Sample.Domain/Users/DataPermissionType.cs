using System.ComponentModel;

namespace SeventyTwo.Sample.Domain.Users;

/// <summary>
/// 数据权限类型。
/// </summary>
public enum DataPermissionType : short
{
    /// <summary>
    /// 全部数据。
    /// </summary>
    [Description("全部数据")]
    All = 1,

    /// <summary>
    /// 本机构数据。
    /// </summary>
    [Description("本机构数据")]
    Organization = 2,

    /// <summary>
    /// 本机构与下级机构数据。
    /// </summary>
    [Description("本机构与下级机构数据")]
    OrganizationAndDescendants = 3,

    /// <summary>
    /// 自己的数据。
    /// </summary>
    [Description("自己的数据")]
    Self = 4,
}
