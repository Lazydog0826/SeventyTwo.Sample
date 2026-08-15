using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Contracts.Products;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 商品类目管理接口。
/// </summary>
/// <param name="productCategoryApplication">商品类目应用服务。</param>
[ApiController]
[Route("api/productCategories")]
public sealed class ProductCategoriesController(IProductCategoryApplication productCategoryApplication) : ControllerBase
{
    /// <summary>
    /// 获取类目编辑详情。
    /// </summary>
    [HttpGet("detail")]
    [Permission(PermissionMatchMode.All, "productCategoriesUpdate")]
    public async Task<IActionResult> GetDetailAsync([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var result = await productCategoryApplication.GetDetailAsync(id, cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 获取类目列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有未删除的类目。</returns>
    [HttpGet("list")]
    [Permission(PermissionMatchMode.All, "productCategories")]
    public async Task<IActionResult> GetListAsync(CancellationToken cancellationToken)
    {
        var result = await productCategoryApplication.GetListAsync(cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 创建类目。
    /// </summary>
    /// <param name="request">类目创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的类目信息。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "productCategoriesCreate")]
    public async Task<IActionResult> CreateAsync(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await productCategoryApplication.CreateAsync(
            new CreateProductCategoryInput(request.Name, request.ParentId),
            cancellationToken
        );
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 修改类目。
    /// </summary>
    /// <param name="request">类目修改请求，包含客户端持有的并发版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "productCategoriesUpdate")]
    public async Task<IActionResult> UpdateAsync(
        UpdateProductCategoryRequest request,
        CancellationToken cancellationToken
    )
    {
        await productCategoryApplication.UpdateAsync(
            request.Id,
            new UpdateProductCategoryInput(request.Name, request.ParentId, request.Version),
            cancellationToken
        );
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 删除无下级类目的类目。
    /// </summary>
    /// <param name="request">类目删除请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "productCategoriesDelete")]
    public async Task<IActionResult> DeleteAsync(
        DeleteProductCategoryRequest request,
        CancellationToken cancellationToken
    )
    {
        await productCategoryApplication.DeleteAsync(request.Id, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }
}
