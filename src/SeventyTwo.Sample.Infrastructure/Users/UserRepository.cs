using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Users;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Users;

[AutofacDependency(typeof(IUserRepository))]
public sealed class UserRepository(ISqlSugarClient db) : IUserRepository
{
    private ISqlSugarClient Db { get; } = db;
}
