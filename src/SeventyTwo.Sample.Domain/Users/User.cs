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
        string phone,
        string email,
        Guid? defaultPageId = null
    )
        : this(id, username, passwordHash, displayName, phone, email, defaultPageId, false) { }

    private User(
        Guid id,
        string username,
        string passwordHash,
        string displayName,
        string phone,
        string email,
        Guid? defaultPageId,
        bool restore
    )
    {
        if (id == Guid.Empty)
        {
            throw new UserDomainException(MessageKeys.Users.IdRequired);
        }

        Id = id;
        Username = RequireText(username, MessageKeys.Users.UsernameRequired);
        if (!restore && string.Equals(Username, SystemUsernames.SuperAdmin, StringComparison.Ordinal))
        {
            throw new UserDomainException(MessageKeys.Users.UsernameReserved);
        }
        PasswordHash = RequireText(passwordHash, MessageKeys.Users.PasswordHashRequired);
        DisplayName = RequireText(displayName, MessageKeys.Users.DisplayNameRequired);
        Phone = RequireText(phone, MessageKeys.Users.PhoneRequired);
        Email = RequireText(email, MessageKeys.Users.EmailRequired);
        DefaultPageId = NormalizeOptionalId(defaultPageId);
    }

    internal static User Restore(
        Guid id,
        string username,
        string passwordHash,
        string displayName,
        string phone,
        string email,
        Guid? defaultPageId = null
    ) => new(id, username, passwordHash, displayName, phone, email, defaultPageId, true);

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
    public string Phone { get; private set; } = string.Empty;

    /// <summary>
    /// 电子邮箱。
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// 登录后默认跳转的页面权限 ID。
    /// </summary>
    public Guid? DefaultPageId { get; private set; }

    public void UpdateProfile(
        string displayName,
        string phone,
        string email,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    ) => UpdateProfile(displayName, phone, email, DefaultPageId, version, updatedBy, updatedAt);

    public void UpdateProfile(
        string displayName,
        string phone,
        string email,
        Guid? defaultPageId,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        ValidateMutation(version, updatedAt);
        DisplayName = RequireText(displayName, MessageKeys.Users.DisplayNameRequired);
        Phone = RequireText(phone, MessageKeys.Users.PhoneRequired);
        Email = RequireText(email, MessageKeys.Users.EmailRequired);
        DefaultPageId = NormalizeOptionalId(defaultPageId);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public void SetEnable(bool enable, Guid version, Guid updatedBy, DateTimeOffset updatedAt)
    {
        ValidateMutation(version, updatedAt);
        Enable = enable;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 重置密码摘要，并通过并发版本防止覆盖其他用户变更。
    /// </summary>
    public void ResetPassword(string passwordHash, Guid version, Guid updatedBy, DateTimeOffset updatedAt)
    {
        ValidateMutation(version, updatedAt);
        PasswordHash = RequireText(passwordHash, MessageKeys.Users.PasswordHashRequired);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public void EnsureCanDelete(Guid version)
    {
        EnsureMutable();
        if (version != Version)
        {
            throw new UserDomainException(MessageKeys.Users.DataChanged, DomainErrorType.Conflict);
        }
    }

    private void ValidateMutation(Guid version, DateTimeOffset updatedAt)
    {
        EnsureMutable();
        if (version != Version)
        {
            throw new UserDomainException(MessageKeys.Users.DataChanged, DomainErrorType.Conflict);
        }
        if (updatedAt == default)
        {
            throw new UserDomainException(MessageKeys.Users.ModifiedAtRequired);
        }
    }

    private void EnsureMutable()
    {
        if (string.Equals(Username, SystemUsernames.SuperAdmin, StringComparison.Ordinal))
        {
            throw new UserDomainException(MessageKeys.Users.SuperAdminProtected, DomainErrorType.Conflict);
        }
    }

    private static string RequireText(string value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new UserDomainException(message) : value.Trim();
    }

    private static Guid? NormalizeOptionalId(Guid? value) => value == Guid.Empty ? null : value;
}
