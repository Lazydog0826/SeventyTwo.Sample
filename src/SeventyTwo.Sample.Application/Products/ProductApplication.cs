using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

[AutofacDependency(typeof(IProductApplication))]
public sealed class ProductApplication(
    IProductRepository productRepository,
    IProductCategoryRepository productCategoryRepository
) : IProductApplication
{
    /// <inheritdoc />
    public async Task<ProductOutput> CreateAsync(CreateProductInput input, CancellationToken cancellationToken)
    {
        var product = new Product(
            Guid.CreateVersion7(),
            input.Name,
            input.Price,
            input.Code,
            input.Description,
            input.Unit,
            input.CategoryId,
            input.Status
        );
        await ValidateCodeAndCategoryAsync(input.Code, null, input.CategoryId, cancellationToken);
        await productRepository.AddAsync(product, cancellationToken);
        return product.Adapt<ProductOutput>();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdateProductInput input, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        await ValidateCodeAndCategoryAsync(input.Code, id, input.CategoryId, cancellationToken);
        product.Update(
            input.Name,
            input.Price,
            input.Code,
            input.Description,
            input.Unit,
            input.CategoryId,
            input.Status,
            input.Version,
            SystemIds.System,
            DateTimeExtension.Now()
        );
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeStatusAsync(
        Guid id,
        ProductStatus status,
        Guid version,
        CancellationToken cancellationToken
    )
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.ChangeStatus(status, version, SystemIds.System, DateTimeExtension.Now());
        await productRepository.SaveAsync(product, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        product.EnsureCanDelete(version);
        await productRepository.DeleteAsync(id, version, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductOutput> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        return product.Adapt<ProductOutput>();
    }

    /// <inheritdoc />
    public async Task<PageResponse<ProductOutput>> GetPageAsync(
        ProductPageRequest request,
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

        if (!request.IsOffsetWithinRange())
        {
            throw new ProductDomainException(MessageKeys.Paging.PageOffsetOutOfRange);
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

    /// <summary>
    /// 校验商品编码未被其他未删除商品占用，且所属类目存在；编码唯一性并发场景由数据库唯一索引兜底。
    /// </summary>
    /// <param name="code">商品编码，校验前先去除首尾空格，与领域层落库口径一致。</param>
    /// <param name="excludeId">需要排除的商品 ID；新增时为 <see langword="null"/>。</param>
    /// <param name="categoryId">所属类目 ID；未归属类目时为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task ValidateCodeAndCategoryAsync(
        string code,
        Guid? excludeId,
        Guid? categoryId,
        CancellationToken cancellationToken
    )
    {
        if (await productRepository.CodeExistsAsync(code.Trim(), excludeId, cancellationToken))
        {
            throw new ProductDomainException(MessageKeys.Products.CodeExists, DomainErrorType.Conflict);
        }

        if (
            categoryId is not null
            && await productCategoryRepository.FindAsync(categoryId.Value, cancellationToken) is null
        )
        {
            throw new ProductDomainException(MessageKeys.Products.CategoryNotFound, DomainErrorType.NotFound);
        }
    }
}
