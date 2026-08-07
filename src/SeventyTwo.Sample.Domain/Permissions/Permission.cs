// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class Permission : AggregateRoot
{
    private Permission() { }

    public Permission(Guid id, string code, string name)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("权限 ID 不能为空");
        }

        Id = id;
        Code = RequireText(code, "权限编码不能为空");
        Name = RequireText(name, "权限名称不能为空");
    }

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
