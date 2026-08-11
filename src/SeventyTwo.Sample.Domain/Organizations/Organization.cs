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

        Id = id;
        Code = RequireText(code, MessageKeys.Organizations.CodeRequired);
        Name = RequireText(name, MessageKeys.Organizations.NameRequired);
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
