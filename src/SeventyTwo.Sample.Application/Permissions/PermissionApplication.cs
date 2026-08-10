using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

[AutofacDependency(typeof(IPermissionApplication))]
public sealed class PermissionApplication(
    IPermissionRepository permissionRepository,
    PermissionMemoryCacheService memoryCacheService,
    IUserPermissionChecker userPermissionChecker,
    IPermissionCacheInvalidationPublisher cacheInvalidationPublisher,
    IUnitOfWork unitOfWork
) : IPermissionApplication
{
    /// <inheritdoc />
    public async Task<PermissionListOutput> CreateAsync(
        CreatePermissionInput input,
        CancellationToken cancellationToken
    )
    {
        var permission = new Permission(
            Guid.CreateVersion7(),
            input.Code,
            input.Title,
            input.Type,
            input.SortOrder,
            input.Icon,
            input.VueComponentPath,
            input.RoutePath,
            input.RouteName,
            input.ParentId,
            input.MetaData
        )
        {
            Enable = input.Enable,
        };
        await ValidateCodeAsync(permission.Code, null, cancellationToken);
        await ValidateParentAsync(permission.Id, permission.ParentId, cancellationToken);
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await permissionRepository.AddAsync(permission, cancellationToken);
                await cacheInvalidationPublisher.PublishAsync(cancellationToken);
            },
            cancellationToken
        );
        return ToListOutput(permission);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdatePermissionInput input, CancellationToken cancellationToken)
    {
        var permission = await GetRequiredAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            throw new PermissionDomainException("权限编码不能为空");
        }

        await ValidateCodeAsync(input.Code.Trim(), id, cancellationToken);
        await ValidateParentAsync(id, input.ParentId, cancellationToken);
        permission.Update(
            input.Code,
            input.Title,
            input.Type,
            input.Enable,
            input.SortOrder,
            input.Icon,
            input.VueComponentPath,
            input.RoutePath,
            input.RouteName,
            input.ParentId,
            input.MetaData,
            input.Version,
            SystemIds.System,
            DateTimeExtension.Now()
        );
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await permissionRepository.SaveAsync(permission, cancellationToken);
                await cacheInvalidationPublisher.PublishAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(id, cancellationToken);
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await permissionRepository.DeleteAsync(id, cancellationToken);
                await cacheInvalidationPublisher.PublishAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.GetListAsync(cancellationToken);
        return permissions.Select(ToListOutput).ToList();
    }

    /// <inheritdoc />
    public async Task<PermissionOutput> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var allPermissions = await GetAllPermissionsAsync(cancellationToken);
        var permissionCodes = await userPermissionChecker.GetCodesAsync(userId, cancellationToken);
        var permissionCodeSet = permissionCodes.ToHashSet(StringComparer.Ordinal);
        var permissions = allPermissions.Where(x => permissionCodeSet.Contains(x.Code)).ToList();
        var menus = permissions
            .Where(x => x.Type is PermissionType.Directory or PermissionType.Page)
            .Select(x => new PermissionMenuOutput
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
                Type = x.Type,
                SortOrder = x.SortOrder,
                Icon = x.Icon,
                VueComponentPath = x.VueComponentPath,
                RoutePath = x.RoutePath,
                RouteName = x.RouteName,
                MetaData = x.MetaData,
                ParentId = x.ParentId,
            })
            .ToList();

        var buttonCodes = permissions
            .Where(x => x.Type == PermissionType.Button)
            .Select(x => x.Code)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return new PermissionOutput(menus, buttonCodes);
    }

    private async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        return await memoryCacheService.GetOrLoadAsync(permissionRepository.GetAllAsync, cancellationToken);
    }

    private async Task<Permission> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException("权限 ID 不能为空");
        }

        return await permissionRepository.FindAsync(id, cancellationToken)
            ?? throw new PermissionDomainException("权限不存在");
    }

    private async Task ValidateCodeAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await permissionRepository.CodeExistsAsync(code, excludedId, cancellationToken))
        {
            throw new PermissionDomainException("权限编码已存在");
        }
    }

    private async Task ValidateParentAsync(Guid id, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        var permissions = await permissionRepository.GetListAsync(cancellationToken);
        var byId = permissions.ToDictionary(permission => permission.Id);
        if (!byId.ContainsKey(parentId.Value))
        {
            throw new PermissionDomainException("上级权限不存在");
        }

        // 从候选上级持续向根节点回溯；遇到当前权限说明本次修改会形成环。
        // visited 同时防止历史异常数据中的既有环导致无限循环。
        var currentId = parentId;
        var visited = new HashSet<Guid>();
        while (currentId is not null)
        {
            if (!visited.Add(currentId.Value))
            {
                throw new PermissionDomainException("权限层级存在循环引用");
            }

            if (currentId == id)
            {
                throw new PermissionDomainException("权限不能以自身或下级权限作为上级权限");
            }

            currentId = byId.TryGetValue(currentId.Value, out var current) ? current.ParentId : null;
        }
    }

    private static PermissionListOutput ToListOutput(Permission permission)
    {
        return new PermissionListOutput
        {
            Id = permission.Id,
            Code = permission.Code,
            Title = permission.Title,
            Type = permission.Type,
            Enable = permission.Enable,
            SortOrder = permission.SortOrder,
            Icon = permission.Icon,
            VueComponentPath = permission.VueComponentPath,
            RoutePath = permission.RoutePath,
            RouteName = permission.RouteName,
            MetaData = permission.MetaData,
            ParentId = permission.ParentId,
            Version = permission.Version,
        };
    }
}
