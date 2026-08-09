using Mapster;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Infrastructure.Permissions;

public sealed class PermissionMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<PermissionRecord, Permission>()
            .ConstructUsing(x => new Permission(
                x.Id,
                x.Code,
                x.Title,
                x.Type,
                x.SortOrder,
                x.Icon,
                x.VueComponentPath,
                x.RoutePath,
                x.RouteName,
                x.ParentId,
                x.MetaData
            ))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
    }
}
