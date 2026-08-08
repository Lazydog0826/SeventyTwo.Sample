using System.ComponentModel.DataAnnotations;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Users;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserApplication userApplication) : ControllerBase
{
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest rqRequest)
    {
        var data = await userApplication.LoginAsync(rqRequest.Adapt<LoginInput>());
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken);
    }

    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var data = await userApplication.RefreshTokenAsync(Request.Cookies["refresh_token"] ?? string.Empty);
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken);
    }

    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task LogoutAsync()
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

public sealed record LoginRequest(
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须为 3～50 个字符")]
        string Account,
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须为 6～100 个字符")]
        string Password
);
