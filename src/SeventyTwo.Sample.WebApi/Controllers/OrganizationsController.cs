using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Organizations;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 机构管理接口。
/// </summary>
/// <param name="organizationApplication">机构应用服务。</param>
[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(IOrganizationApplication organizationApplication) : ControllerBase
{
    /// <summary>
    /// 获取机构编辑详情。
    /// </summary>
    [HttpGet("detail")]
    [Permission(PermissionMatchMode.All, "organizationsUpdate")]
    public async Task<IActionResult> GetDetailAsync([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var result = await organizationApplication.GetDetailAsync(id, cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 获取机构管理列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除的机构。</returns>
    [HttpGet("list")]
    [Permission(PermissionMatchMode.All, "organizationsList")]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var result = await organizationApplication.GetListAsync(cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 创建机构。
    /// </summary>
    /// <param name="request">机构创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的机构信息。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "organizationsCreate")]
    public async Task<IActionResult> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await organizationApplication.CreateAsync(
            new CreateOrganizationInput(
                request.Code,
                request.Name,
                request.Enable,
                request.ParentId,
                request.SortOrder
            ),
            cancellationToken
        );
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 修改机构。
    /// </summary>
    /// <param name="request">机构修改请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "organizationsUpdate")]
    public async Task<IActionResult> UpdateAsync(UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        await organizationApplication.UpdateAsync(
            request.Id,
            new UpdateOrganizationInput(
                request.Code,
                request.Name,
                request.Enable,
                request.ParentId,
                request.Version,
                request.SortOrder
            ),
            cancellationToken
        );
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 删除无下级且无成员的机构。
    /// </summary>
    /// <param name="request">机构删除请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "organizationsDelete")]
    public async Task<IActionResult> DeleteAsync(DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        await organizationApplication.DeleteAsync(request.Id, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }
}

/// <summary>
/// 机构创建请求。
/// </summary>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID；为空时创建根机构。</param>
/// <param name="SortOrder">排序号。</param>
public record CreateOrganizationRequest(string Code, string Name, bool Enable, Guid? ParentId, int SortOrder = 0);

/// <summary>
/// 机构修改请求。
/// </summary>
/// <param name="Id">机构 ID。</param>
/// <param name="Code">机构编码。</param>
/// <param name="Name">机构名称。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="ParentId">上级机构 ID。</param>
/// <param name="Version">客户端持有的并发版本。</param>
/// <param name="SortOrder">排序号。</param>
public sealed record UpdateOrganizationRequest(
    Guid Id,
    string Code,
    string Name,
    bool Enable,
    Guid? ParentId,
    Guid Version,
    int SortOrder = 0
) : CreateOrganizationRequest(Code, Name, Enable, ParentId, SortOrder);

/// <summary>
/// 机构删除请求。
/// </summary>
/// <param name="Id">机构 ID。</param>
public sealed record DeleteOrganizationRequest(Guid Id);
