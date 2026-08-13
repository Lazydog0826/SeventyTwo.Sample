using Mapster;
using SeventyTwo.Sample.Domain.Permissions;

namespace SeventyTwo.Sample.Application.Permissions;

/// <summary>
/// 配置权限聚合到应用层输出模型的映射。
/// </summary>
public sealed class PermissionOutputMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Permission, PermissionListOutput>();
        config.NewConfig<Permission, PermissionMenuOutput>();
        config.NewConfig<Permission, DefaultPageOptionOutput>();
    }
}
