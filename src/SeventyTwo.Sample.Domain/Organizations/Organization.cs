// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Organizations;

public sealed class Organization : AggregateRoot
{
    /// <summary>
    /// 供持久化组件还原机构聚合使用。
    /// </summary>
    private Organization() { }

    /// <summary>
    /// 创建机构聚合。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="code">机构编码。</param>
    /// <param name="name">机构名称。</param>
    /// <param name="parentId">上级机构 ID；根机构为 <see langword="null"/>。</param>
    /// <param name="path"></param>
    /// <param name="sortOrder"></param>
    public Organization(
        Guid id,
        string code,
        string name,
        Guid? parentId = null,
        string? path = null,
        int sortOrder = 0
    )
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.IdRequired);
        }

        if (parentId == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ParentIdRequired);
        }

        if (parentId == id)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.SelfCannotBeParent);
        }

        ValidateSortOrder(sortOrder);

        Id = id;
        Code = RequireText(code, MessageKeys.Organizations.CodeRequired);
        Name = RequireText(name, MessageKeys.Organizations.NameRequired);
        ParentId = parentId;
        Path = parentId is null ? id.ToString() : RequirePath(path);
        SortOrder = sortOrder;
    }

    /// <summary>
    /// 机构编码。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 机构名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 上级机构 ID；根机构为 <see langword="null"/>。
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 由机构 ID 组成的完整层级路径。
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 修改机构信息，并校验客户端持有的并发版本。
    /// </summary>
    /// <param name="code">机构编码。</param>
    /// <param name="name">机构名称。</param>
    /// <param name="enable">是否启用。</param>
    /// <param name="parentId">上级机构 ID；根机构为 <see langword="null"/>。</param>
    /// <param name="version">客户端读取机构时获得的版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    /// <param name="sortOrder"></param>
    public void Update(
        string code,
        string name,
        bool enable,
        Guid? parentId,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt,
        int sortOrder = 0
    )
    {
        if (version != Version)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ModifiedAtRequired);
        }

        if (parentId == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ParentIdRequired);
        }

        if (parentId == Id)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.SelfCannotBeParent);
        }
        ValidateSortOrder(sortOrder);

        Code = RequireText(code, MessageKeys.Organizations.CodeRequired);
        Name = RequireText(name, MessageKeys.Organizations.NameRequired);
        Enable = enable;
        ParentId = parentId;
        SortOrder = sortOrder;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 根据新的上级机构更新层级路径。
    /// </summary>
    public void ChangePath(string parentPath)
    {
        Path = $"{RequirePath(parentPath)}/{Id}";
    }

    /// <summary>
    /// 校验并规范化必填文本。
    /// </summary>
    /// <param name="value">待校验的文本。</param>
    /// <param name="message">校验失败时使用的消息键。</param>
    /// <returns>去除首尾空白后的文本。</returns>
    private static string RequireText(string value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new OrganizationDomainException(message) : value.Trim();
    }

    private static string RequirePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("上级机构 Path 不能为空。", nameof(path))
            : path;
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.SortMustNotBeNegative);
        }
    }
}
