// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Organizations;

public sealed class Organization : AggregateRoot
{
    private Organization() { }

    public Organization(Guid id, string code, string name, Guid? parentId = null)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("机构 ID 不能为空");
        }

        if (parentId == Guid.Empty)
        {
            throw new OrganizationDomainException("上级机构 ID 不能为空");
        }

        if (parentId == id)
        {
            throw new OrganizationDomainException("机构不能以自身作为上级机构");
        }

        Id = id;
        Code = RequireText(code, "机构编码不能为空");
        Name = RequireText(name, "机构名称不能为空");
        ParentId = parentId;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OrganizationDomainException(message);
        }

        return value.Trim();
    }
}
