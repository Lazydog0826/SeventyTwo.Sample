using Mapster;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.DataDictionaries;
using SeventyTwo.Sample.WebApi.Contracts.DataDictionaries;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>数据字典管理接口。</summary>
[ApiController]
[Route("api/dataDictionaries")]
public sealed class DataDictionariesController(IDataDictionaryApplication application) : ControllerBase
{
    /// <summary>获取字典管理列表。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>字典列表。</returns>
    [HttpGet("list")]
    [Permission(PermissionMatchMode.All, "dataDictionariesList")]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken) =>
        WebApiResponse.Query(await application.GetListAsync(cancellationToken), message: MessageKeys.Common.Success);

    /// <summary>获取指定字典的字典项。</summary>
    /// <param name="id">字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>字典项及并发版本。</returns>
    [HttpGet("{id:guid}/items")]
    [Permission(PermissionMatchMode.All, "dataDictionariesList")]
    public async Task<IActionResult> GetItemsAsync(Guid id, CancellationToken cancellationToken) =>
        WebApiResponse.Query(
            await application.GetItemsAsync(id, cancellationToken),
            message: MessageKeys.Common.Success
        );

    /// <summary>创建字典。</summary>
    /// <param name="request">创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的字典。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "dataDictionariesCreate")]
    public async Task<IActionResult> CreateAsync(
        CreateDataDictionaryRequest request,
        CancellationToken cancellationToken
    ) =>
        WebApiResponse.Query(
            await application.CreateAsync(request.Adapt<CreateDataDictionaryInput>(), cancellationToken),
            message: MessageKeys.Common.Success
        );

    /// <summary>更新字典。</summary>
    /// <param name="request">更新请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "dataDictionariesUpdate")]
    public async Task<IActionResult> UpdateAsync(
        UpdateDataDictionaryRequest request,
        CancellationToken cancellationToken
    )
    {
        await application.UpdateAsync(request.Id, request.Adapt<UpdateDataDictionaryInput>(), cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>删除字典及其字典项。</summary>
    /// <param name="request">删除请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "dataDictionariesDelete")]
    public async Task<IActionResult> DeleteAsync(
        DeleteDataDictionaryRequest request,
        CancellationToken cancellationToken
    )
    {
        await application.DeleteAsync(request.Id, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>创建字典项。</summary>
    /// <param name="request">创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>字典项及新版本。</returns>
    [HttpPost("items/create")]
    [Permission(PermissionMatchMode.All, "dataDictionariesUpdate")]
    public async Task<IActionResult> CreateItemAsync(
        CreateDataDictionaryItemRequest request,
        CancellationToken cancellationToken
    ) =>
        WebApiResponse.Query(
            await application.CreateItemAsync(request.Adapt<CreateDataDictionaryItemInput>(), cancellationToken),
            message: MessageKeys.Common.Success
        );

    /// <summary>更新字典项。</summary>
    /// <param name="request">更新请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>字典项及新版本。</returns>
    [HttpPost("items/update")]
    [Permission(PermissionMatchMode.All, "dataDictionariesUpdate")]
    public async Task<IActionResult> UpdateItemAsync(
        UpdateDataDictionaryItemRequest request,
        CancellationToken cancellationToken
    ) =>
        WebApiResponse.Query(
            await application.UpdateItemAsync(request.Adapt<UpdateDataDictionaryItemInput>(), cancellationToken),
            message: MessageKeys.Common.Success
        );

    /// <summary>删除字典项。</summary>
    /// <param name="request">删除请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新版本。</returns>
    [HttpPost("items/delete")]
    [Permission(PermissionMatchMode.All, "dataDictionariesUpdate")]
    public async Task<IActionResult> DeleteItemAsync(
        DeleteDataDictionaryItemRequest request,
        CancellationToken cancellationToken
    ) =>
        WebApiResponse.Query(
            await application.DeleteItemAsync(request.Adapt<DeleteDataDictionaryItemInput>(), cancellationToken),
            message: MessageKeys.Common.Success
        );

    /// <summary>按字典编码获取业务选项。</summary>
    /// <param name="code">字典编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按排序号排列的选项。</returns>
    [HttpGet("by-code/{code}/items")]
    public async Task<IActionResult> GetOptionsByCodeAsync(string code, CancellationToken cancellationToken) =>
        WebApiResponse.Query(
            await application.GetOptionsByCodeAsync(code, cancellationToken),
            message: MessageKeys.Common.Success
        );
}
