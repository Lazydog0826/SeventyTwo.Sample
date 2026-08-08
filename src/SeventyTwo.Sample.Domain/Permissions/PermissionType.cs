using System.ComponentModel;

namespace SeventyTwo.Sample.Domain.Permissions;

/// <summary>
/// 权限类型。
/// </summary>
public enum PermissionType : short
{
    /// <summary>
    /// 目录。
    /// </summary>
    [Description("目录")]
    Directory = 1,

    /// <summary>
    /// 页面。
    /// </summary>
    [Description("页面")]
    Page = 2,

    /// <summary>
    /// 按钮。
    /// </summary>
    [Description("按钮")]
    Button = 3,
}
