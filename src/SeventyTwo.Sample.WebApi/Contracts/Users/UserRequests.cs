using System.ComponentModel.DataAnnotations;

// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.WebApi.Contracts.Users;

/// <summary>用户授权保存请求。</summary>
/// <param name="UserId">目标用户 ID。</param>
/// <param name="PermissionIds">完整权限 ID 集合；空值按空集合处理。</param>
public sealed record AuthorizeUserRequest(Guid UserId, IReadOnlyList<Guid>? PermissionIds);

/// <summary>
/// 用户登录请求。
/// </summary>
/// <param name="Account">账号。</param>
/// <param name="Password">密码。</param>
public sealed record LoginRequest(
    [Required(ErrorMessage = MessageKeys.Validation.AccountRequired)]
    [StringLength(50, MinimumLength = 3, ErrorMessage = MessageKeys.Validation.AccountLengthInvalid)]
        string Account,
    [Required(ErrorMessage = MessageKeys.Validation.PasswordRequired)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = MessageKeys.Validation.PasswordLengthInvalid)]
        string Password
);

/// <summary>
/// 用户创建请求。
/// </summary>
/// <param name="Username">用户名。</param>
/// <param name="Password">密码。</param>
/// <param name="DisplayName">显示名称。</param>
/// <param name="Phone">手机号。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="OrgId">所属机构 ID。</param>
/// <param name="DefaultPageId">登录后默认页面权限 ID。</param>
public sealed record CreateUserRequest(
    [Required(ErrorMessage = MessageKeys.Validation.AccountRequired)]
    [StringLength(50, MinimumLength = 3, ErrorMessage = MessageKeys.Validation.AccountLengthInvalid)]
        string Username,
    [Required(ErrorMessage = MessageKeys.Validation.PasswordRequired)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = MessageKeys.Validation.PasswordLengthInvalid)]
        string Password,
    [Required(ErrorMessage = MessageKeys.Users.DisplayNameRequired)] string DisplayName,
    [Required(ErrorMessage = MessageKeys.Users.PhoneRequired)] string Phone,
    [Required(ErrorMessage = MessageKeys.Users.EmailRequired)] string Email,
    bool Enable,
    Guid OrgId,
    Guid? DefaultPageId
);

/// <summary>
/// 用户修改请求。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="DisplayName">显示名称。</param>
/// <param name="Phone">手机号。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="OrgId">所属机构 ID。</param>
/// <param name="Version">客户端持有的并发版本。</param>
/// <param name="DefaultPageId">登录后默认页面权限 ID。</param>
public sealed record UpdateUserRequest(
    Guid Id,
    [Required(ErrorMessage = MessageKeys.Users.DisplayNameRequired)] string DisplayName,
    [Required(ErrorMessage = MessageKeys.Users.PhoneRequired)] string Phone,
    [Required(ErrorMessage = MessageKeys.Users.EmailRequired)] string Email,
    Guid OrgId,
    Guid Version,
    Guid? DefaultPageId
);

/// <summary>
/// 用户启用状态设置请求。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record SetUserEnableRequest(Guid Id, bool Enable, Guid Version);

/// <summary>
/// 用户删除请求。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record DeleteUserRequest(Guid Id, Guid Version);

/// <summary>
/// 用户密码重置请求。
/// </summary>
public sealed record ResetPasswordRequest(Guid Id, Guid Version);
