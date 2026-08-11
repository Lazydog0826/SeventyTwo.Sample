using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Domain.Permissions;
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
    /// 创建权限。
    /// </summary>
    /// <param name="request">权限创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的权限信息。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "Permissions.Create")]
    public async Task<IActionResult> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await permissionApplication.CreateAsync(
            new CreatePermissionInput(
                request.Code,
                request.Title,
                request.Type,
                request.Enable,
                request.SortOrder,
                request.Icon,
                request.VueComponentPath,
                request.RoutePath,
                request.RouteName,
                request.ParentId,
                request.MetaData
            ),
            cancellationToken
        );
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 修改权限。
    /// </summary>
    /// <param name="request">权限修改请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "Permissions.Update")]
    public async Task<IActionResult> UpdateAsync(UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        await permissionApplication.UpdateAsync(
            request.Id,
            new UpdatePermissionInput(
                request.Code,
                request.Title,
                request.Type,
                request.Enable,
                request.SortOrder,
                request.Icon,
                request.VueComponentPath,
                request.RoutePath,
                request.RouteName,
                request.ParentId,
                request.MetaData,
                request.Version
            ),
            cancellationToken
        );
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 物理删除权限及其全部用户权限关联。
    /// </summary>
    /// <param name="request">权限删除请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    /// <remarks>存在下级权限时拒绝删除。</remarks>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "Permissions.Delete")]
    public async Task<IActionResult> DeleteAsync(DeletePermissionRequest request, CancellationToken cancellationToken)
    {
        await permissionApplication.DeleteAsync(request.Id, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

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
        return WebApiResponse.Query(res, message: MessageKeys.Common.Success);
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
        return WebApiResponse.Query(res, message: MessageKeys.Common.Success);
    }
}

/// <summary>
/// 权限创建请求。
/// </summary>
/// <param name="Code">权限编码。</param>
/// <param name="Title">权限标题。</param>
/// <param name="Type">权限类型。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="Icon">图标。</param>
/// <param name="VueComponentPath">Vue 组件路径。</param>
/// <param name="RoutePath">路由路径。</param>
/// <param name="RouteName">路由名称。</param>
/// <param name="ParentId">上级权限 ID。</param>
/// <param name="MetaData">路由元数据。</param>
public record CreatePermissionRequest(
    string Code,
    string Title,
    PermissionType Type,
    bool Enable,
    int SortOrder,
    string? Icon,
    string? VueComponentPath,
    string? RoutePath,
    string? RouteName,
    Guid? ParentId,
    PermissionMetaData? MetaData
);

/// <summary>
/// 权限修改请求。
/// </summary>
/// <param name="Id">权限 ID。</param>
/// <param name="Code">权限编码。</param>
/// <param name="Title">权限标题。</param>
/// <param name="Type">权限类型。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="Icon">图标。</param>
/// <param name="VueComponentPath">Vue 组件路径。</param>
/// <param name="RoutePath">路由路径。</param>
/// <param name="RouteName">路由名称。</param>
/// <param name="ParentId">上级权限 ID。</param>
/// <param name="MetaData">路由元数据。</param>
/// <param name="Version">客户端持有的并发版本。</param>
public sealed record UpdatePermissionRequest(
    Guid Id,
    string Code,
    string Title,
    PermissionType Type,
    bool Enable,
    int SortOrder,
    string? Icon,
    string? VueComponentPath,
    string? RoutePath,
    string? RouteName,
    Guid? ParentId,
    PermissionMetaData? MetaData,
    Guid Version
)
    : CreatePermissionRequest(
        Code,
        Title,
        Type,
        Enable,
        SortOrder,
        Icon,
        VueComponentPath,
        RoutePath,
        RouteName,
        ParentId,
        MetaData
    );

/// <summary>
/// 权限删除请求。
/// </summary>
/// <param name="Id">权限 ID。</param>
public sealed record DeletePermissionRequest(Guid Id);
