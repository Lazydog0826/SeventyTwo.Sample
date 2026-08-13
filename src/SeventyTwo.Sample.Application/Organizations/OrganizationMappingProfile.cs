using Mapster;
using SeventyTwo.Sample.Domain.Organizations;

namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>
/// 配置机构聚合到应用层输出模型的映射。
/// </summary>
public sealed class OrganizationOutputMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Organization, OrganizationListOutput>();
    }
}
