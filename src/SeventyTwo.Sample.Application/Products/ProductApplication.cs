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
        var product = new Product(Guid.CreateVersion7(), input.Name, input.Price);
        await productRepository.AddAsync(product, cancellationToken);
        return product.Adapt<ProductOutput>();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdateProductInput input, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.Update(input.Name, input.Price, input.Version, SystemIds.System, DateTimeExtension.Now());
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.Delete(SystemIds.System, DateTimeExtension.Now());
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductOutput> GetAsync(Guid id, CancellationToken cancellationToken)
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
            throw new ProductDomainException(MessageKeys.Paging.PageNumberMustBePositive);
        }

        if (request.Limit is <= 0 or > 100)
        {
            throw new ProductDomainException(MessageKeys.Paging.PageSizeOutOfRange100);
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
    private async Task<Product> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.Products.IdRequired);
        }

        return await productRepository.FindAsync(id, cancellationToken) ?? throw new ProductNotFoundException();
    }
}
