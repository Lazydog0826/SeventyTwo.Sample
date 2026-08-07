// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class Role : AggregateRoot
{
    private Role() { }

    public Role(Guid id, Guid organizationId, string code, string name)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("角色 ID 不能为空");
        }

        if (organizationId == Guid.Empty)
        {
            throw new PermissionDomainException("机构 ID 不能为空");
        }

        Id = id;
        OrganizationId = organizationId;
        Code = RequireText(code, "角色编码不能为空");
        Name = RequireText(name, "角色名称不能为空");
    }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PermissionDomainException(message);
        }

        return value.Trim();
    }
}
