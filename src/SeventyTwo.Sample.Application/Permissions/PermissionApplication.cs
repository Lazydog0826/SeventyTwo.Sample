using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 权限应用服务。
/// </summary>
[AutofacDependency(typeof(IPermissionApplication))]
public sealed class PermissionApplication(
    IPermissionRepository permissionRepository,
    PermissionCacheService cacheService,
    IUserPermissionCacheService userPermissionCacheService,
    IPermissionCacheInvalidationPublisher cacheInvalidationPublisher,
    IUserPermissionCacheInvalidationPublisher userPermissionCacheInvalidationPublisher,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository
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
                await userPermissionCacheInvalidationPublisher.PublishAsync(Guid.Empty, true, cancellationToken);
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
            throw new PermissionDomainException(MessageKeys.Permissions.CodeRequired);
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
                await userPermissionCacheInvalidationPublisher.PublishAsync(Guid.Empty, true, cancellationToken);
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
                await userPermissionCacheInvalidationPublisher.PublishAsync(Guid.Empty, true, cancellationToken);
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.GetListAsync(cancellationToken);
        return [.. permissions.Select(ToListOutput)];
    }

    /// <inheritdoc />
    public async Task<PermissionOutput> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var allPermissions = await GetAllPermissionsAsync(cancellationToken);
        var permissionCodes = await userPermissionCacheService.GetCodesAsync(userId, cancellationToken);
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

    /// <inheritdoc />
    public async Task<UserAuthorizationOutput> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken)
    {
        await ValidateAuthorizableUserAsync(userId, cancellationToken);
        var permissions = await permissionRepository.GetListAsync(cancellationToken);
        var associatedIds = await permissionRepository.GetIdsByUserIdAsync(userId, cancellationToken);
        return new UserAuthorizationOutput(
            permissions.Select(ToListOutput).ToList(),
            associatedIds.Distinct().ToList()
        );
    }

    /// <inheritdoc />
    public async Task AuthorizeAsync(
        Guid userId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken
    )
    {
        if (permissionIds.Count != permissionIds.Distinct().Count())
            throw new PermissionDomainException(MessageKeys.Permissions.AuthorizationInvalid);

        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await userRepository.AcquireSecurityLockAsync(userId, cancellationToken);
                await ValidateAuthorizableUserAsync(userId, cancellationToken);
                var permissions = await permissionRepository.GetListAsync(cancellationToken);
                var byId = permissions.ToDictionary(x => x.Id);
                foreach (var permissionId in permissionIds)
                {
                    if (!byId.TryGetValue(permissionId, out var permission))
                        throw new PermissionDomainException(MessageKeys.Permissions.AuthorizationInvalid);
                    for (
                        var parentId = permission.ParentId;
                        parentId.HasValue;
                        parentId = byId[parentId.Value].ParentId
                    )
                    {
                        if (!byId.ContainsKey(parentId.Value) || !permissionIds.Contains(parentId.Value))
                            throw new PermissionDomainException(MessageKeys.Permissions.AuthorizationHierarchyInvalid);
                    }
                }
                await permissionRepository.ReplaceUserPermissionsAsync(userId, permissionIds, cancellationToken);
                await userPermissionCacheInvalidationPublisher.PublishAsync(userId, false, cancellationToken);
            },
            cancellationToken
        );
    }

    /// <summary>
    /// 验证目标用户允许被授权，并拒绝空 ID、不存在用户和超级管理员。
    /// </summary>
    /// <param name="userId">目标用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task ValidateAuthorizableUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new PermissionDomainException(MessageKeys.Users.IdRequired);
        var user =
            await userRepository.GetAsync(userId, cancellationToken)
            ?? throw new PermissionDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
        if (string.Equals(user.Username, SystemUsernames.SuperAdmin, StringComparison.Ordinal))
            throw new PermissionDomainException(
                MessageKeys.Permissions.SuperAdminAuthorizationForbidden,
                DomainErrorType.Conflict
            );
    }

    /// <summary>
    /// 获取所有权限，并使用内存缓存复用查询结果。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>所有权限。</returns>
    private async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        return await cacheService.GetOrLoadAsync(permissionRepository.GetAllAsync, cancellationToken);
    }

    /// <summary>
    /// 查询指定权限，不存在时抛出业务异常。
    /// </summary>
    /// <param name="id">权限 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>权限聚合。</returns>
    private async Task<Permission> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new PermissionDomainException(MessageKeys.Permissions.IdRequired);
        }

        return await permissionRepository.FindAsync(id, cancellationToken)
            ?? throw new PermissionDomainException(MessageKeys.Permissions.NotFound, DomainErrorType.NotFound);
    }

    /// <summary>
    /// 验证权限编码未被其他权限使用。
    /// </summary>
    /// <param name="code">权限编码。</param>
    /// <param name="excludedId">校验时排除的权限 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task ValidateCodeAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await permissionRepository.CodeExistsAsync(code, excludedId, cancellationToken))
        {
            throw new PermissionDomainException(MessageKeys.Permissions.CodeExists, DomainErrorType.Conflict);
        }
    }

    /// <summary>
    /// 验证上级权限存在，且设置后不会形成循环引用。
    /// </summary>
    /// <param name="id">当前权限 ID。</param>
    /// <param name="parentId">候选上级权限 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
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
            throw new PermissionDomainException(MessageKeys.Permissions.ParentNotFound, DomainErrorType.NotFound);
        }

        // 从候选上级持续向根节点回溯；遇到当前权限说明本次修改会形成环。
        // visited 同时防止历史异常数据中的既有环导致无限循环。
        var currentId = parentId;
        var visited = new HashSet<Guid>();
        while (currentId is not null)
        {
            if (!visited.Add(currentId.Value))
            {
                throw new PermissionDomainException(MessageKeys.Permissions.HierarchyCycle);
            }

            if (currentId == id)
            {
                throw new PermissionDomainException(MessageKeys.Permissions.DescendantCannotBeParent);
            }

            currentId = byId.TryGetValue(currentId.Value, out var current) ? current.ParentId : null;
        }
    }

    /// <summary>
    /// 将权限聚合转换为权限管理列表项。
    /// </summary>
    /// <param name="permission">权限聚合。</param>
    /// <returns>权限管理列表项。</returns>
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
