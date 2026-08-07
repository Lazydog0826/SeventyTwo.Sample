using SeventyTwo.Sample.Infrastructure.Persistence;
using SqlSugar;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Users;

[SugarTable("user_account")]
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
    [SugarColumn(ColumnName = "phone", IsNullable = true)]
    public string? Phone { get; init; }

    /// <summary>
    /// 电子邮箱。
    /// </summary>
    [SugarColumn(ColumnName = "email", IsNullable = true)]
    public string? Email { get; init; }
}
