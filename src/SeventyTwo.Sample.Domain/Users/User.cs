// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.Domain.Users;

public sealed class User : AggregateRoot
{
    private User() { }

    public User(
        Guid id,
        string username,
        string passwordHash,
        string displayName,
        string? phone = null,
        string? email = null
    )
    {
        if (id == Guid.Empty)
        {
            throw new UserDomainException("用户 ID 不能为空");
        }

        Id = id;
        Username = RequireText(username, "用户名不能为空");
        PasswordHash = RequireText(passwordHash, "密码摘要不能为空");
        DisplayName = RequireText(displayName, "用户姓名不能为空");
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
    }

    /// <summary>
    /// 用户名。
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// 密码摘要。
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// 用户姓名。
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// 手机号码。
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// 电子邮箱。
    /// </summary>
    public string? Email { get; private set; }

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UserDomainException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
