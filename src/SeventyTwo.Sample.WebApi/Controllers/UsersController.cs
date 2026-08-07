using System.ComponentModel.DataAnnotations;
using Mapster;
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
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest rqRequest)
    {
        var data = await userApplication.LoginAsync(rqRequest.Adapt<LoginInput>());
        return WebApiResponse.Query(data.AccessToken);
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
