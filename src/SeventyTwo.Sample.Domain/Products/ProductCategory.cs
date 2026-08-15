// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Products;

/// <summary>
/// 商品类目实体，支持通过父类目 ID 组成树形结构。
/// </summary>
public sealed class ProductCategory : AggregateRoot
{
    /// <summary>
    /// 供持久化组件还原商品类目使用。
    /// </summary>
    private ProductCategory() { }

    /// <summary>
    /// 创建商品类目。
    /// </summary>
    /// <param name="id">类目 ID。</param>
    /// <param name="name">类目名称。</param>
    /// <param name="parentId">上级类目 ID；顶级类目为 <see langword="null"/>。</param>
    /// <param name="path">上级类目的层级路径，顶级类目无需传入。</param>
    public ProductCategory(Guid id, string name, Guid? parentId = null, string? path = null)
    {
        if (id == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.IdRequired);
        }

        Id = id;
        Enable = true;
        SetInfo(name, parentId);
        Path = parentId is null ? id.ToString() : RequirePath(path);
    }

    /// <summary>
    /// 类目名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 上级类目 ID；顶级类目为 <see langword="null"/>。
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 由类目 ID 组成的完整层级路径。
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// 修改类目基础信息。
    /// </summary>
    /// <param name="name">类目名称。</param>
    /// <param name="parentId">上级类目 ID；顶级类目为 <see langword="null"/>。</param>
    /// <param name="version">客户端持有的类目版本 UUIDv7。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    public void Update(string name, Guid? parentId, Guid version, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (version != Version)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.ModifiedAtRequired);
        }

        SetInfo(name, parentId);
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 将类目标记为已删除。
    /// </summary>
    /// <param name="deletedBy">删除人 ID。</param>
    /// <param name="deletedAt">删除时间。</param>
    public void Delete(Guid deletedBy, DateTimeOffset deletedAt)
    {
        if (deletedAt == default)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.DeletedAtRequired);
        }

        Enable = false;
        DeleteBy = deletedBy;
        DeleteAt = deletedAt;
    }

    /// <summary>
    /// 根据新的上级类目更新层级路径；上级为空时重置为根路径。
    /// </summary>
    /// <param name="parentPath">上级类目的层级路径。</param>
    public void ChangePath(string parentPath)
    {
        Path = string.IsNullOrEmpty(parentPath) ? Id.ToString() : $"{RequirePath(parentPath)}/{Id}";
    }

    /// <summary>
    /// 校验并设置类目基础信息。
    /// </summary>
    /// <param name="name">类目名称。</param>
    /// <param name="parentId">上级类目 ID；顶级类目为 <see langword="null"/>。</param>
    private void SetInfo(string name, Guid? parentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.NameRequired);
        }

        name = name.Trim();
        if (name.Length > 255)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.NameTooLong);
        }

        if (parentId == Guid.Empty)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.ParentIdRequired);
        }

        if (parentId == Id)
        {
            throw new ProductDomainException(MessageKeys.ProductCategories.SelfCannotBeParent);
        }

        Name = name;
        ParentId = parentId;
    }

    /// <summary>
    /// 校验上级类目层级路径不能为空。
    /// </summary>
    /// <param name="path">上级类目的层级路径。</param>
    /// <returns>校验通过的层级路径。</returns>
    private static string RequirePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("上级类目 Path 不能为空。", nameof(path))
            : path;
    }
}
