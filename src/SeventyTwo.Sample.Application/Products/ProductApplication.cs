using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

[AutofacDependency(typeof(IProductApplication))]
public sealed class ProductApplication(IProductRepository productRepository) : IProductApplication
{
    /// <inheritdoc />
    public async Task<ProductOutput> CreateAsync(CreateProductInput input, CancellationToken cancellationToken)
    {
        var product = new Product(Yitter.IdGenerator.YitIdHelper.NextId(), input.Name, input.Price);
        await productRepository.AddAsync(product, cancellationToken);
        return product.Adapt<ProductOutput>();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(long id, UpdateProductInput input, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.Update(input.Name, input.Price, input.Version, 0, DateTimeExtension.Now());
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.Delete(0, DateTimeExtension.Now());
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductOutput> GetAsync(long id, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        return product.Adapt<ProductOutput>();
    }

    /// <inheritdoc />
    public async Task<PageResponse<ProductOutput>> GetPageAsync(
        PageRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Index <= 0)
        {
            throw new ProductDomainException("页码必须大于 0");
        }

        if (request.Limit is <= 0 or > 100)
        {
            throw new ProductDomainException("每页数量必须在 1 到 100 之间");
        }

        var page = await productRepository.GetPageAsync(request, cancellationToken);
        return new PageResponse<ProductOutput> { List = page.Items.Adapt<List<ProductOutput>>(), Total = page.Total };
    }

    /// <summary>
    /// 查询指定商品，不存在时抛出业务异常。
    /// </summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>商品聚合。</returns>
    private async Task<Product> GetRequiredAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ProductDomainException("商品 ID 必须大于 0");
        }

        return await productRepository.FindAsync(id, cancellationToken) ?? throw new ProductNotFoundException();
    }
}
