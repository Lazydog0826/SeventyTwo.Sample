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
        string icon,
        string vueComponentPath,
        string routePath,
        string routeName,
        Guid? parentId,
        PermissionMetaData? metaData = null,
        string path = ""
    )
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.IdRequired);
        }

        Id = id;
        Enable = true;
        Version = Guid.CreateVersion7();
        SetInfo(code, title, type, sortOrder, icon, vueComponentPath, routePath, routeName, parentId, metaData);
        Path = parentId is null ? id.ToString() : RequirePath(path);
    }

    /// <summary>
    /// 修改权限信息，并校验客户端持有的并发版本。
    /// </summary>
    /// <param name="metaData"></param>
    /// <param name="version">客户端读取权限时获得的版本。</param>
    /// <param name="updatedBy">修改人 ID。</param>
    /// <param name="updatedAt">修改时间。</param>
    /// <param name="code"></param>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="enable"></param>
    /// <param name="sortOrder"></param>
    /// <param name="icon"></param>
    /// <param name="vueComponentPath"></param>
    /// <param name="routePath"></param>
    /// <param name="routeName"></param>
    /// <param name="parentId"></param>
    public void Update(
        string code,
        string title,
        PermissionType type,
        bool enable,
        int sortOrder,
        string icon,
        string vueComponentPath,
        string routePath,
        string routeName,
        Guid? parentId,
        PermissionMetaData? metaData,
        Guid version,
        Guid updatedBy,
        DateTimeOffset updatedAt
    )
    {
        if (version != Version)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.DataChanged, DomainErrorType.Conflict);
        }

        if (updatedAt == default)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.ModifiedAtRequired);
        }

        SetInfo(code, title, type, sortOrder, icon, vueComponentPath, routePath, routeName, parentId, metaData);
        Enable = enable;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    private void SetInfo(
        string code,
        string title,
        PermissionType type,
        int sortOrder,
        string icon,
        string vueComponentPath,
        string routePath,
        string routeName,
        Guid? parentId,
        PermissionMetaData? metaData
    )
    {
        if (!Enum.IsDefined(type))
        {
            throw new PermissionDomainException(MessageKeys.Permissions.TypeInvalid);
        }

        if (sortOrder < 0)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.SortMustNotBeNegative);
        }

        if (parentId == Guid.Empty)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.ParentIdRequired);
        }

        if (parentId == Id)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.SelfCannotBeParent);
        }

        Code = RequireText(code, MessageKeys.Permissions.CodeRequired);
        Title = RequireText(title, MessageKeys.Permissions.TitleRequired);
        Type = type;
        SortOrder = sortOrder;
        ParentId = parentId;

        switch (type)
        {
            case PermissionType.Directory:
                Icon = RequireText(icon, MessageKeys.Permissions.DirectoryIconRequired);
                VueComponentPath = NormalizeOptionalText(vueComponentPath);
                RoutePath = NormalizeOptionalText(routePath);
                RouteName = NormalizeOptionalText(routeName);
                break;
            case PermissionType.Page:
                Icon = NormalizeOptionalText(icon);
                VueComponentPath = RequireText(vueComponentPath, MessageKeys.Permissions.VueComponentPathRequired);
                RoutePath = RequireText(routePath, MessageKeys.Permissions.RoutePathRequired);
                RouteName = RequireText(routeName, MessageKeys.Permissions.RouteNameRequired);
                break;
            case PermissionType.Button:
                if (parentId is null)
                {
                    throw new PermissionDomainException(MessageKeys.Permissions.ButtonParentRequired);
                }

                Icon = NormalizeOptionalText(icon);
                VueComponentPath = NormalizeOptionalText(vueComponentPath);
                RoutePath = NormalizeOptionalText(routePath);
                RouteName = NormalizeOptionalText(routeName);
                break;
            default:
                throw new PermissionDomainException(MessageKeys.Permissions.TypeInvalid);
        }

        MetaData =
            type == PermissionType.Button
                ? metaData ?? default
                : metaData ?? throw new PermissionDomainException(MessageKeys.Permissions.RouteMetadataRequired);
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
    /// 由权限 ID 组成的完整层级路径。
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// 路由元数据。
    /// </summary>
    public PermissionMetaData MetaData { get; private set; }

    /// <summary>
    /// 根据新的上级权限更新层级路径；上级为空时重置为根路径。
    /// </summary>
    public void ChangePath(string parentPath)
    {
        Path = string.IsNullOrEmpty(parentPath) ? Id.ToString() : $"{RequirePath(parentPath)}/{Id}";
    }

    private static string RequireText(string value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new PermissionDomainException(message) : value.Trim();
    }

    private static string NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string RequirePath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("上级权限 Path 不能为空。", nameof(path))
            : path;
    }
}
