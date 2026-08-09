using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Users;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 用户接口。
/// </summary>
/// <param name="userApplication">用户应用服务。</param>
[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserApplication userApplication) : ControllerBase
{
    /// <summary>
    /// 获取当前登录用户信息。
    /// </summary>
    /// <returns>当前登录用户信息。</returns>
    [HttpGet("Info")]
    public async Task<IActionResult> GetInfoAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var data = await userApplication.GetAsync(userId);
        return WebApiResponse.Query(data);
    }

    /// <summary>
    /// 用户登录。
    /// </summary>
    /// <param name="rqRequest">登录请求。</param>
    /// <returns>访问令牌。</returns>
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest rqRequest)
    {
        var data = await userApplication.LoginAsync(rqRequest.Adapt<LoginInput>());
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken);
    }

    /// <summary>
    /// 刷新访问令牌。
    /// </summary>
    /// <returns>新的访问令牌。</returns>
    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var data = await userApplication.RefreshTokenAsync(Request.Cookies["refresh_token"] ?? string.Empty);
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken);
    }

    /// <summary>
    /// 退出登录。
    /// </summary>
    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoutAsync()
    {
        await userApplication.LogoutAsync(Request.Cookies["refresh_token"] ?? string.Empty);
        Response.Cookies.Delete(
            "refresh_token",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            }
        );
        return WebApiResponse.Operate();
    }

    private void SetRefreshTokenCookie(LoginOutput data)
    {
        Response.Cookies.Append(
            "refresh_token",
            data.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = data.ExpireTime,
                Path = "/",
            }
        );
    }
}

/// <summary>
/// 用户登录请求。
/// </summary>
/// <param name="Account">账号。</param>
/// <param name="Password">密码。</param>
public sealed record LoginRequest(
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须为 3～50 个字符")]
        string Account,
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须为 6～100 个字符")]
        string Password
);
