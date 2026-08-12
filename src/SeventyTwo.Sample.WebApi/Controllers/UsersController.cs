using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Organizations;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.WebApi.Authentication;

// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 用户接口。
/// </summary>
/// <param name="userApplication">用户应用服务。</param>
/// <param name="organizationApplication">机构应用服务。</param>
/// <param name="permissionApplication">权限应用服务。</param>
[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IUserApplication userApplication,
    IOrganizationApplication organizationApplication,
    IPermissionApplication permissionApplication
) : ControllerBase
{
    /// <summary>
    /// 获取用户管理列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除的用户。</returns>
    [HttpGet("list")]
    [Permission(PermissionMatchMode.All, "usersList")]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var data = await userApplication.GetListAsync(cancellationToken);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 获取用户新增、编辑时可选择的已启用机构。
    /// </summary>
    [HttpGet("organization-options")]
    [Permission(PermissionMatchMode.Any, "usersCreate", "usersUpdate")]
    public async Task<IActionResult> GetOrganizationOptionsAsync(CancellationToken cancellationToken)
    {
        var organizations = await organizationApplication.GetListAsync(cancellationToken);
        var data = organizations.Where(organization => organization.Enable);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 创建用户。
    /// </summary>
    /// <param name="request">用户创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的用户信息。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "usersCreate")]
    public async Task<IActionResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var data = await userApplication.CreateAsync(
            new CreateUserInput(
                request.Username,
                request.Password,
                request.DisplayName,
                request.Phone,
                request.Email,
                request.Enable,
                request.OrgId
            ),
            cancellationToken
        );
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 修改用户信息。
    /// </summary>
    /// <param name="request">用户修改请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "usersUpdate")]
    public async Task<IActionResult> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await userApplication.UpdateAsync(
            request.Id,
            new UpdateUserInput(request.DisplayName, request.Phone, request.Email, request.OrgId, request.Version),
            cancellationToken
        );
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 设置用户启用状态。
    /// </summary>
    /// <param name="request">用户启用状态设置请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("set-enable")]
    [Permission(PermissionMatchMode.All, "usersUpdate")]
    public async Task<IActionResult> SetEnableAsync(SetUserEnableRequest request, CancellationToken cancellationToken)
    {
        await userApplication.SetEnableAsync(request.Id, new(request.Enable, request.Version), cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 删除用户。
    /// </summary>
    /// <param name="request">用户删除请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "usersDelete")]
    public async Task<IActionResult> DeleteAsync(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        await userApplication.DeleteAsync(request.Id, request.Version, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>获取指定用户的授权编辑数据。</summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>授权编辑数据。</returns>
    [HttpGet("authorization")]
    [Permission(PermissionMatchMode.All, "usersAuthorize")]
    public async Task<IActionResult> GetAuthorizationAsync([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        var data = await permissionApplication.GetAuthorizationAsync(userId, cancellationToken);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

    /// <summary>整体保存指定用户的权限。</summary>
    /// <param name="request">用户授权保存请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("authorize")]
    [Permission(PermissionMatchMode.All, "usersAuthorize")]
    public async Task<IActionResult> AuthorizeAsync(AuthorizeUserRequest request, CancellationToken cancellationToken)
    {
        await permissionApplication.AuthorizeAsync(request.UserId, request.PermissionIds ?? [], cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 获取当前登录用户信息。
    /// </summary>
    /// <param name="cancellationToken">用于取消用户信息查询的令牌。</param>
    /// <returns>当前登录用户信息。</returns>
    [HttpGet("Info")]
    public async Task<IActionResult> GetInfoAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var data = await userApplication.GetAsync(userId, cancellationToken);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 用户登录。
    /// </summary>
    /// <param name="rqRequest">登录请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>访问令牌。</returns>
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest rqRequest, CancellationToken cancellationToken)
    {
        var data = await userApplication.LoginAsync(rqRequest.Adapt<LoginInput>(), cancellationToken);
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 刷新访问令牌。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新的访问令牌。</returns>
    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        var data = await userApplication.RefreshTokenAsync(
            Request.Cookies["refresh_token"] ?? string.Empty,
            cancellationToken
        );
        SetRefreshTokenCookie(data);
        return WebApiResponse.Query(data.AccessToken, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 退出登录。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        await userApplication.LogoutAsync(Request.Cookies["refresh_token"] ?? string.Empty, cancellationToken);
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
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 将刷新令牌写入响应 Cookie。
    /// </summary>
    /// <param name="data">登录结果。</param>
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
    Guid OrgId
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
public sealed record UpdateUserRequest(
    Guid Id,
    [Required(ErrorMessage = MessageKeys.Users.DisplayNameRequired)] string DisplayName,
    [Required(ErrorMessage = MessageKeys.Users.PhoneRequired)] string Phone,
    [Required(ErrorMessage = MessageKeys.Users.EmailRequired)] string Email,
    Guid OrgId,
    Guid Version
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
