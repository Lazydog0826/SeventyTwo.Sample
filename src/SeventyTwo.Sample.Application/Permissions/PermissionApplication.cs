using Microsoft.Extensions.Caching.Memory;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

[AutofacDependency(typeof(IPermissionApplication))]
public sealed class PermissionApplication(
    IPermissionRepository permissionRepository,
    IMemoryCache memoryCache,
    IUserPermissionChecker userPermissionChecker
) : IPermissionApplication
{
    private const string AllPermissionsCacheKey = "Permissions:All";
    private static readonly TimeSpan AllPermissionsSlidingExpiration = TimeSpan.FromMinutes(30);

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
        if (
            memoryCache.TryGetValue<IReadOnlyList<Permission>>(AllPermissionsCacheKey, out var cachedPermissions)
            && cachedPermissions is not null
        )
        {
            return cachedPermissions;
        }

        var permissions = await permissionRepository.GetAllAsync(cancellationToken);
        return memoryCache.Set(
            AllPermissionsCacheKey,
            permissions,
            new MemoryCacheEntryOptions { SlidingExpiration = AllPermissionsSlidingExpiration }
        );
    }
}
