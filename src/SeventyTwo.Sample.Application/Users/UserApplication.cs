using Mapster;
using Microsoft.AspNetCore.Identity;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Users;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户应用服务。
/// </summary>
[AutofacDependency(typeof(IUserApplication))]
public sealed class UserApplication(
    IUserRepository userRepository,
    UserInfoCacheService userInfoCacheService,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IUserTokenCacheService userTokenCacheService
) : IUserApplication
{
    public async Task<IReadOnlyList<UserListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetListAsync(cancellationToken);
        return [.. users.Select(ToListOutput)];
    }

    public async Task<UserListOutput> CreateAsync(CreateUserInput input, CancellationToken cancellationToken)
    {
        User? user = null;
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var username = RequireText(input.Username, MessageKeys.Users.UsernameRequired);
                var password = RequirePassword(input.Password);
                if (await userRepository.UsernameExistsAsync(username, cancellationToken))
                {
                    throw new UserDomainException(MessageKeys.Users.UsernameExists, DomainErrorType.Conflict);
                }
                var passwordHash = new PasswordHasher<string>().HashPassword(username, password);
                user = new User(
                    Guid.CreateVersion7(),
                    username,
                    passwordHash,
                    input.DisplayName,
                    input.Phone,
                    input.Email
                )
                {
                    Enable = input.Enable,
                    OrgId = Guid.Empty,
                };
                await userRepository.AddAsync(user, cancellationToken);
            },
            cancellationToken
        );
        return ToListOutput(user!);
    }

    public async Task UpdateAsync(Guid id, UpdateUserInput input, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var user = await GetRequiredAsync(id, cancellationToken);
                user.UpdateProfile(
                    input.DisplayName,
                    input.Phone,
                    input.Email,
                    input.Version,
                    SystemIds.System,
                    DateTimeExtension.Now()
                );
                await userRepository.SaveAsync(user, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task SetEnableAsync(Guid id, SetUserEnableInput input, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var user = await GetRequiredAsync(id, cancellationToken);
                user.SetEnable(input.Enable, input.Version, SystemIds.System, DateTimeExtension.Now());
                await userRepository.SaveAsync(user, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                var user = await GetRequiredAsync(id, cancellationToken);
                user.EnsureCanDelete(version);
                await userRepository.DeleteAsync(id, version, cancellationToken);
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<UserOutput> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await userInfoCacheService.FindAsync(id, cancellationToken)
            ?? throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
    }

    /// <inheritdoc />
    public async Task<LoginOutput> LoginAsync(LoginInput request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByAccountAsync(request.Account, cancellationToken);
        if (user == null)
        {
            throw new UserDomainException(MessageKeys.Users.CredentialsInvalid);
        }

        var valid = new PasswordHasher<string>().VerifyHashedPassword(
            request.Account,
            user.PasswordHash,
            request.Password
        );

        if (valid.Equals(PasswordVerificationResult.Failed))
        {
            throw new UserDomainException(MessageKeys.Users.CredentialsInvalid);
        }

        var sessionId = Guid.CreateVersion7();
        var tokens = tokenService.Generate(user, sessionId);

        if (!await userTokenCacheService.SaveAsync(user.Id, sessionId, tokens, cancellationToken))
        {
            throw new InvalidOperationException("保存登录会话失败");
        }

        return tokens.Adapt<LoginOutput>();
    }

    /// <inheritdoc />
    public async Task<LoginOutput> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (
            string.IsNullOrWhiteSpace(refreshToken)
            || !tokenService.TryValidate(refreshToken, out var payload)
            || payload is not { TokenType: "refresh" }
            || !await userTokenCacheService.IsTokenIssuedAfterInvalidBeforeAsync(
                payload.UserId,
                payload.IssuedAtUnixTimeSeconds,
                cancellationToken
            )
        )
        {
            throw new TokenAuthenticationException(MessageKeys.Authentication.RefreshTokenInvalid);
        }

        var user = await userRepository.GetAsync(payload.UserId, cancellationToken);
        if (user == null)
        {
            throw new TokenAuthenticationException(MessageKeys.Authentication.RefreshTokenInvalid);
        }

        var tokens = tokenService.Generate(user, payload.SessionId);
        if (
            !await userTokenCacheService.RefreshAsync(
                payload.UserId,
                payload.SessionId,
                payload.IssuedAtUnixTimeSeconds,
                refreshToken,
                tokens,
                cancellationToken
            )
        )
        {
            throw new TokenAuthenticationException(MessageKeys.Authentication.RefreshTokenInvalid);
        }

        return tokens.Adapt<LoginOutput>();
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (
            string.IsNullOrWhiteSpace(refreshToken)
            || !tokenService.TryValidate(refreshToken, out var payload)
            || payload is not { TokenType: "refresh" }
        )
        {
            return;
        }

        await userTokenCacheService.DeleteAsync(payload.UserId, payload.SessionId, refreshToken, cancellationToken);
    }

    private async Task<User> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            throw new UserDomainException(MessageKeys.Users.IdRequired);
        return await userRepository.GetAsync(id, cancellationToken)
            ?? throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
    }

    private static string RequireText(string value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new UserDomainException(message) : value.Trim();

    private static string RequirePassword(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new UserDomainException(MessageKeys.Validation.PasswordRequired)
            : value;

    private static UserListOutput ToListOutput(User user) =>
        new(user.Id, user.Username, user.DisplayName, user.Phone, user.Email, user.Enable, user.Version);
}
