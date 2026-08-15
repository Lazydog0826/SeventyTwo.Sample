using Mapster;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Contracts.Products;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 商品接口。
/// </summary>
/// <param name="productApplication">商品应用服务。</param>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductApplication productApplication) : ControllerBase
{
    /// <summary>
    /// 创建商品。
    /// </summary>
    /// <param name="request">创建商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的商品信息。</returns>
    [HttpPost("create")]
    [Permission(PermissionMatchMode.All, "productsCreate")]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await productApplication.CreateAsync(request.Adapt<CreateProductInput>(), cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 修改商品。
    /// </summary>
    /// <param name="request">修改商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("update")]
    [Permission(PermissionMatchMode.All, "productsUpdate")]
    public async Task<IActionResult> Update(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await productApplication.UpdateAsync(request.Id, request.Adapt<UpdateProductInput>(), cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 切换商品上架状态。
    /// </summary>
    /// <param name="request">商品上架状态切换请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("change-status")]
    [Permission(PermissionMatchMode.All, "productsUpdate")]
    public async Task<IActionResult> ChangeStatus(
        ChangeProductStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        await productApplication.ChangeStatusAsync(request.Id, request.Status, request.Version, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 删除商品。
    /// </summary>
    /// <param name="request">删除商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("delete")]
    [Permission(PermissionMatchMode.All, "productsDelete")]
    public async Task<IActionResult> Delete(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        await productApplication.DeleteAsync(request.Id, request.Version, cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 查询商品详情。
    /// </summary>
    /// <param name="request">查询商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品信息。</returns>
    [HttpPost("get")]
    [Permission(PermissionMatchMode.All, "productsUpdate")]
    public async Task<IActionResult> Get(GetProductRequest request, CancellationToken cancellationToken)
    {
        var result = await productApplication.GetAsync(request.Id, cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }

    /// <summary>
    /// 分页查询商品。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    [HttpPost("page")]
    [Permission(PermissionMatchMode.All, "productsList")]
    public async Task<IActionResult> GetPage(ProductPageRequest request, CancellationToken cancellationToken)
    {
        var result = await productApplication.GetPageAsync(request, cancellationToken);
        return WebApiResponse.Query(result, message: MessageKeys.Common.Success);
    }
}
