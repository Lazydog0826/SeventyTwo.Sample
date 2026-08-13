// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户登录输入。
/// </summary>
/// <param name="Account">用户名。</param>
/// <param name="Password">密码。</param>
public sealed record LoginInput(string Account, string Password);

/// <summary>
/// 用户登录输出。
/// </summary>
/// <param name="AccessToken">访问令牌。</param>
/// <param name="RefreshToken">刷新令牌。</param>
/// <param name="ExpireTime">刷新令牌过期时间。</param>
public sealed record LoginOutput(string AccessToken, string RefreshToken, DateTime ExpireTime);

/// <summary>
/// 用户信息输出。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="Username">用户名。</param>
/// <param name="DisplayName">用户姓名。</param>
/// <param name="Phone">手机号码。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="DefaultPagePath">登录后默认页面路由；配置无效时为空字符串。</param>
public sealed record UserOutput(
    Guid Id,
    string Username,
    string DisplayName,
    string Phone,
    string Email,
    string DefaultPagePath = ""
);

/// <summary>
/// 用户列表项。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="Username">用户名。</param>
/// <param name="DisplayName">显示名称。</param>
/// <param name="Phone">手机号码。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="OrgId">所属机构 ID。</param>
/// <param name="Version">并发版本。</param>
public sealed record UserListOutput(
    Guid Id,
    string Username,
    string DisplayName,
    string Phone,
    string Email,
    bool Enable,
    Guid OrgId,
    Guid Version,
    Guid? DefaultPageId = null
);

/// <summary>
/// 创建用户的输入。
/// </summary>
/// <param name="Username">用户名。</param>
/// <param name="Password">密码。</param>
/// <param name="DisplayName">显示名称。</param>
/// <param name="Phone">手机号码。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="OrgId">所属机构 ID。</param>
public sealed record CreateUserInput(
    string Username,
    string Password,
    string DisplayName,
    string Phone,
    string Email,
    bool Enable,
    Guid OrgId,
    Guid? DefaultPageId = null
);

/// <summary>
/// 更新用户的输入。
/// </summary>
/// <param name="DisplayName">显示名称。</param>
/// <param name="Phone">手机号码。</param>
/// <param name="Email">电子邮箱。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record UpdateUserInput(
    string DisplayName,
    string Phone,
    string Email,
    Guid OrgId,
    Guid Version,
    Guid? DefaultPageId = null
);

/// <summary>
/// 用户启用状态设置输入。
/// </summary>
/// <param name="Enable">是否启用。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record SetUserEnableInput(bool Enable, Guid Version);
