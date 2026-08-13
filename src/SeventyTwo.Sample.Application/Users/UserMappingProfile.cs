using Mapster;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 配置用户聚合到应用层输出模型的映射。
/// </summary>
public sealed class UserOutputMappingProfile : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserListOutput>();
    }
}
