using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

[AutofacDependency(typeof(IProductCategoryApplication))]
public sealed class ProductCategoryApplication(
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork
) : IProductCategoryApplication
{
    public async Task<ProductCategoryListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken) =>
        (await GetRequiredAsync(id, cancellationToken)).Adapt<ProductCategoryListOutput>();

    public async Task<ProductCategoryListOutput> CreateAsync(
        CreateProductCategoryInput input,
        CancellationToken cancellationToken
    )
    {
        var id = Guid.CreateVersion7();
        ProductCategory? category = null;
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await productCategoryRepository.AcquireMutationLockAsync(cancellationToken);
                var parent = input.ParentId is null
                    ? null
                    : await productCategoryRepository.FindAsync(input.ParentId.Value, cancellationToken)
                        ?? throw new ProductDomainException(
                            MessageKeys.ProductCategories.ParentNotFound,
                            DomainErrorType.NotFound
                        );
                category = new ProductCategory(
                    id,
                    input.Name,
                    input.ParentId,
                    parent is null ? null : $"{parent.Path}/{id}",
                    input.SortOrder
                );
                await productCategoryRepository.AddAsync(category, cancellationToken);
            },
            cancellationToken
        );
        return category!.Adapt<ProductCategoryListOutput>();
    }

    public async Task UpdateAsync(Guid id, UpdateProductCategoryInput input, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.IdRequired);
        }

        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await productCategoryRepository.AcquireMutationLockAsync(cancellationToken);
                var category = await GetRequiredAsync(id, cancellationToken);
                var parent = await ValidateParentChangeAsync(category, input.ParentId, cancellationToken);
                category.Update(
                    input.Name,
                    input.ParentId,
                    input.Version,
                    SystemIds.System,
                    DateTimeExtension.Now(),
                    input.SortOrder
                );
                // 类目允许在顶级与子级之间移动，统一按新上级重算路径，由仓储级联更新后代。
                category.ChangePath(parent?.Path ?? string.Empty);
                await productCategoryRepository.SaveAsync(category, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await productCategoryRepository.AcquireMutationLockAsync(cancellationToken);
                var category = await GetRequiredAsync(id, cancellationToken);
                if (await productCategoryRepository.HasChildrenAsync(id, cancellationToken))
                {
                    throw new ProductDomainException(
                        MessageKeys.ProductCategories.HasChildren,
                        DomainErrorType.Conflict
                    );
                }

                category.Delete(SystemIds.System, DateTimeExtension.Now());
                await productCategoryRepository.SaveAsync(category, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<ProductCategoryListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var categories = await productCategoryRepository.GetListAsync(cancellationToken);
        return categories.Adapt<List<ProductCategoryListOutput>>();
    }

    private async Task<ProductCategory> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.IdRequired);
        }

        return await productCategoryRepository.FindAsync(id, cancellationToken)
            ?? throw new ProductDomainException(MessageKeys.ProductCategories.NotFound, DomainErrorType.NotFound);
    }

    /// <summary>
    /// 校验新的上级类目存在且不在自身后代链上，返回新上级（顶级为 null）。
    /// </summary>
    private async Task<ProductCategory?> ValidateParentChangeAsync(
        ProductCategory category,
        Guid? parentId,
        CancellationToken cancellationToken
    )
    {
        if (parentId is null)
        {
            return null;
        }

        var categories = await productCategoryRepository.GetListAsync(cancellationToken);
        var byId = categories.ToDictionary(item => item.Id);
        if (!byId.TryGetValue(parentId.Value, out var parent))
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.ParentNotFound, DomainErrorType.NotFound);
        }

        var currentId = parentId;
        var visited = new HashSet<Guid>();
        while (currentId is not null)
        {
            if (currentId == category.Id)
            {
                throw new ProductDomainException(MessageKeys.ProductCategories.DescendantCannotBeParent);
            }
            if (!visited.Add(currentId.Value))
            {
                break; // 祖先链成环属数据异常，终止遍历避免死循环。
            }
            currentId = byId.TryGetValue(currentId.Value, out var current) ? current.ParentId : null;
        }
        return parent;
    }
}
