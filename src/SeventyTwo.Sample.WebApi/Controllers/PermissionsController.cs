using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 权限接口。
/// </summary>
/// <param name="permissionApplication">权限应用服务。</param>
[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController(IPermissionApplication permissionApplication) : ControllerBase
{
    /// <summary>
    /// 获取权限管理列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除权限，包含已禁用权限。</returns>
    [HttpGet("list")]
    [Permission(PermissionMatchMode.All, "Permissions.List")]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var res = await permissionApplication.GetListAsync(cancellationToken);
        return WebApiResponse.Query(res);
    }

    /// <summary>
    /// 获取当前登录用户的权限。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目录和页面权限列表以及按钮权限编码集合。</returns>
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var res = await permissionApplication.GetByUserIdAsync(userId, cancellationToken);
        return WebApiResponse.Query(res);
    }
}
