using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Users;

[SugarTable("user_account")]
[SugarIndex("uq_user_account_username", nameof(Username), OrderByType.Asc, true)]
[SugarIndex(
    "ix_user_account_username_password_hash",
    nameof(Username),
    OrderByType.Asc,
    nameof(PasswordHash),
    OrderByType.Asc
)]
internal sealed class UserAccountRecord : BaseEntity
{
    /// <summary>
    /// 用户名。
    /// </summary>
    [SugarColumn(ColumnName = "username")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 密码摘要。
    /// </summary>
    [SugarColumn(ColumnName = "password_hash")]
    public string PasswordHash { get; init; } = string.Empty;

    /// <summary>
    /// 用户姓名。
    /// </summary>
    [SugarColumn(ColumnName = "display_name")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 手机号。
    /// </summary>
    [SugarColumn(ColumnName = "phone")]
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// 电子邮箱。
    /// </summary>
    [SugarColumn(ColumnName = "email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 登录后默认跳转的页面权限 ID。
    /// </summary>
    [SugarColumn(ColumnName = "default_page_id", IsNullable = true, ColumnDataType = "uuid")]
    public Guid? DefaultPageId { get; init; }
}
