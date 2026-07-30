using Mapster;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain;

// ReSharper disable NotAccessedPositionalProperty.Global

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

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
    public Task<ProductOutput> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        return productApplication.CreateAsync(request.Adapt<CreateProductInput>(), cancellationToken);
    }

    /// <summary>
    /// 修改商品。
    /// </summary>
    /// <param name="request">修改商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("update")]
    public Task Update(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        return productApplication.UpdateAsync(request.Id, request.Adapt<UpdateProductInput>(), cancellationToken);
    }

    /// <summary>
    /// 软删除商品。
    /// </summary>
    /// <param name="request">删除商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("delete")]
    public Task Delete(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        return productApplication.DeleteAsync(request.Id, cancellationToken);
    }

    /// <summary>
    /// 查询商品详情。
    /// </summary>
    /// <param name="request">查询商品请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品信息。</returns>
    [HttpPost("get")]
    public Task<ProductOutput> Get(GetProductRequest request, CancellationToken cancellationToken)
    {
        return productApplication.GetAsync(request.Id, cancellationToken);
    }

    /// <summary>
    /// 分页查询商品。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品分页数据。</returns>
    [HttpPost("page")]
    public Task<PageResponse<ProductOutput>> GetPage(PageRequest request, CancellationToken cancellationToken)
    {
        return productApplication.GetPageAsync(request, cancellationToken);
    }
}

public sealed record CreateProductRequest(string Name, decimal Price);

public sealed record UpdateProductRequest(long Id, string Name, decimal Price, long Version);

public sealed record DeleteProductRequest(long Id);

public sealed record GetProductRequest(long Id);
