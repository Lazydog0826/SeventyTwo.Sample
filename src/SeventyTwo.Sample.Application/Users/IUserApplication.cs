// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户应用服务。
/// </summary>
public interface IUserApplication
{
    /// <summary>
    /// 获取用户信息。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">用于取消用户信息查询的令牌。</param>
    /// <returns>用户信息。</returns>
    Task<UserOutput> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 用户登录。
    /// </summary>
    /// <param name="request">登录输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>登录令牌。</returns>
    Task<LoginOutput> LoginAsync(LoginInput request, CancellationToken cancellationToken);

    /// <summary>
    /// 使用刷新令牌轮换访问令牌和刷新令牌。
    /// </summary>
    /// <param name="refreshToken">刷新令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的令牌。</returns>
    Task<LoginOutput> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// 退出当前登录会话。
    /// </summary>
    /// <param name="refreshToken">刷新令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}

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
/// <param name="ExpireTime"></param>
public sealed record LoginOutput(string AccessToken, string RefreshToken, DateTime ExpireTime);

/// <summary>
/// 用户信息输出。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="Username">用户名。</param>
/// <param name="DisplayName">用户姓名。</param>
/// <param name="Phone">手机号码。</param>
/// <param name="Email">电子邮箱。</param>
public sealed record UserOutput(Guid Id, string Username, string DisplayName, string? Phone, string? Email);
