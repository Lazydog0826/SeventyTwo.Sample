using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Users;

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserApplication userApplication) : ControllerBase
{
    private IUserApplication UserApplication { get; } = userApplication;
}
