using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Users;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Users;

[AutofacDependency(typeof(IUserRepository))]
public sealed class UserRepository(ISqlSugarClient db) : IUserRepository
{
    public async Task<User?> GetByAccountAsync(string account)
    {
        var user = await db.Queryable<UserAccountRecord>().Where(x => x.Username == account).FirstAsync();
        return user?.Adapt<User>();
    }
}
