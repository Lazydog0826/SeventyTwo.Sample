using Mapster;
using SeventyTwo.Sample.Domain.Organizations;

namespace SeventyTwo.Sample.Infrastructure.Organizations;

public sealed class OrganizationMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<OrganizationRecord, Organization>()
            .ConstructUsing(x => new Organization(x.Id, x.Code, x.Name, x.ParentId, x.Path, x.SortOrder))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
    }
}
