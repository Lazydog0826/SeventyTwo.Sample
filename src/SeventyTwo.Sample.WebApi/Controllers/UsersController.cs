using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Organizations;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Contracts.Users;

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
    /// 获取用户编辑详情。
    /// </summary>
    [HttpGet("detail")]
    [Permission(PermissionMatchMode.All, "usersUpdate")]
    public async Task<IActionResult> GetDetailAsync([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var data = await userApplication.GetDetailAsync(id, cancellationToken);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
    }

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
    /// 获取用户新增、编辑时可选择的有效默认页面。
    /// </summary>
    [HttpGet("default-page-options")]
    [Permission(PermissionMatchMode.Any, "usersCreate", "usersUpdate")]
    public async Task<IActionResult> GetDefaultPageOptionsAsync(CancellationToken cancellationToken)
    {
        var data = await permissionApplication.GetDefaultPageOptionsAsync(cancellationToken);
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
                request.OrgId,
                request.DefaultPageId
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
            new UpdateUserInput(
                request.DisplayName,
                request.Phone,
                request.Email,
                request.OrgId,
                request.Version,
                request.DefaultPageId
            ),
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
    /// 重置指定用户的密码，并仅在本次响应中返回生成的密码。
    /// </summary>
    [HttpPost("reset-password")]
    [Permission(PermissionMatchMode.All, "usersResetPassword")]
    public async Task<IActionResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        var data = await userApplication.ResetPasswordAsync(request.Id, request.Version, cancellationToken);
        return WebApiResponse.Query(data, message: MessageKeys.Common.Success);
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
                // 当前前后端即使端口不同，只要仍是同一 schemeful site，Lax 即可满足刷新 Cookie。
                // 若生产环境部署为不同站点（不同 scheme 或注册域），规范要求改用 SameSite=None；
                // 同时继续强制 Secure，并将 CORS 精确限制为可信前端来源；优先通过同站反向代理部署。
                SameSite = SameSiteMode.Lax,
                Expires = data.ExpireTime,
                Path = "/",
            }
        );
    }
}
