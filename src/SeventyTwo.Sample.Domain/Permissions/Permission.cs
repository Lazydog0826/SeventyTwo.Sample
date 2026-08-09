// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class Permission : AggregateRoot
{
    private Permission() { }

    public Permission(
        Guid id,
        string code,
        string title,
        PermissionType type,
        int sortOrder,
        string? icon,
        string? vueComponentPath,
        string? routePath,
        string? routeName,
        Guid? parentId,
        PermissionMetaData? metaData = null
    )
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("权限 ID 不能为空");
        }

        if (!Enum.IsDefined(type))
        {
            throw new PermissionDomainException("权限类型无效");
        }

        if (sortOrder < 0)
        {
            throw new PermissionDomainException("权限排序号不能小于 0");
        }

        if (parentId == Guid.Empty)
        {
            throw new PermissionDomainException("上级权限 ID 不能为空");
        }

        if (parentId == id)
        {
            throw new PermissionDomainException("权限不能以自身作为上级权限");
        }

        Id = id;
        Code = RequireText(code, "权限编码不能为空");
        Title = RequireText(title, "权限标题不能为空");
        Type = type;
        SortOrder = sortOrder;
        ParentId = parentId;

        switch (type)
        {
            case PermissionType.Directory:
                Icon = RequireText(icon, "目录图标不能为空");
                VueComponentPath = NormalizeOptionalText(vueComponentPath);
                RoutePath = NormalizeOptionalText(routePath);
                RouteName = NormalizeOptionalText(routeName);
                break;
            case PermissionType.Page:
                Icon = NormalizeOptionalText(icon);
                VueComponentPath = RequireText(vueComponentPath, "页面 Vue 组件路径不能为空");
                RoutePath = RequireText(routePath, "页面路由路径不能为空");
                RouteName = RequireText(routeName, "页面路由名称不能为空");
                break;
            case PermissionType.Button:
                if (parentId is null)
                {
                    throw new PermissionDomainException("按钮的上级权限不能为空");
                }

                Icon = NormalizeOptionalText(icon);
                VueComponentPath = NormalizeOptionalText(vueComponentPath);
                RoutePath = NormalizeOptionalText(routePath);
                RouteName = NormalizeOptionalText(routeName);
                break;
            default:
                throw new PermissionDomainException("权限类型无效");
        }

        MetaData =
            type == PermissionType.Button
                ? metaData ?? default
                : metaData ?? throw new PermissionDomainException("路由元数据不能为空");
    }

    /// <summary>
    /// 权限编码。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 权限标题。
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// 权限类型。
    /// </summary>
    public PermissionType Type { get; private set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 图标。
    /// </summary>
    public string Icon { get; private set; } = string.Empty;

    /// <summary>
    /// Vue 组件路径。
    /// </summary>
    public string VueComponentPath { get; private set; } = string.Empty;

    /// <summary>
    /// 路由路径。
    /// </summary>
    public string RoutePath { get; private set; } = string.Empty;

    /// <summary>
    /// 路由名称。
    /// </summary>
    public string RouteName { get; private set; } = string.Empty;

    /// <summary>
    /// 上级权限 ID。
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 路由元数据。
    /// </summary>
    public PermissionMetaData MetaData { get; private set; }

    private static string RequireText(string? value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new PermissionDomainException(message) : value.Trim();
    }

    private static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
