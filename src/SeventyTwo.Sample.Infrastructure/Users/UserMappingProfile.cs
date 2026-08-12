using Mapster;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Infrastructure.Users;

public sealed class UserMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<UserAccountRecord, User>()
            .ConstructUsing(x => User.Restore(x.Id, x.Username, x.PasswordHash, x.DisplayName, x.Phone, x.Email))
            .AfterMapping((source, destination) => source.AggregateRootToEntity(destination));
    }
}
