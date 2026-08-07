using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Application.Users;

[AutofacDependency(typeof(IUserApplication))]
public sealed class UserApplication(IUserRepository userRepository) : IUserApplication
{
    private IUserRepository UserRepository { get; } = userRepository;
}
