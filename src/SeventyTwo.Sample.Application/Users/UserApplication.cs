using System.Security.Cryptography;
using Mapster;
using Microsoft.AspNetCore.Identity;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Organizations;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Domain.Users;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace SeventyTwo.Sample.Application.Users;

/// <summary>
/// 用户应用服务。
/// </summary>
[AutofacDependency(typeof(IUserApplication))]
public sealed class UserApplication(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    UserInfoCacheService userInfoCacheService,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IUserTokenCacheService userTokenCacheService,
    IUserInfoCacheInvalidationPublisher userInfoCacheInvalidationPublisher,
    IPermissionRepository permissionRepository
) : IUserApplication
{
    private const string PasswordUppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string PasswordLowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string PasswordDigits = "23456789";
    private const string PasswordSpecial = "!@#$%^&*-_+";

    public async Task<UserListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetRequiredAsync(id, cancellationToken);
        if (string.Equals(user.Username, SystemUsernames.SuperAdmin, StringComparison.Ordinal))
        {
            throw new UserDomainException(MessageKeys.Users.SuperAdminProtected, DomainErrorType.Conflict);
        }
        return user.Adapt<UserListOutput>();
    }

    public async Task<PageResponse<UserListOutput>> GetPageAsync(
        UserPageRequest request,
        CancellationToken cancellationToken
    )
    {
        ValidatePageRequest(request);
        var page = await userRepository.GetPageAsync(request, cancellationToken);
        return new PageResponse<UserListOutput> { List = page.Items.Adapt<List<UserListOutput>>(), Total = page.Total };
    }

    /// <summary>
    /// 校验用户管理列表的分页参数。
    /// </summary>
    private static void ValidatePageRequest(PageRequest request)
    {
        if (request.Index <= 0)
            throw new UserDomainException(MessageKeys.Paging.PageNumberMustBePositive);
        if (request.Limit is <= 0 or > 100)
            throw new UserDomainException(MessageKeys.Paging.PageSizeOutOfRange100);
        if (!request.IsOffsetWithinRange())
            throw new UserDomainException(MessageKeys.Paging.PageOffsetOutOfRange);
    }

    public async Task<UserListOutput> CreateAsync(CreateUserInput input, CancellationToken cancellationToken)
    {
        var username = RequireText(input.Username, MessageKeys.Users.UsernameRequired);
        var password = RequirePassword(input.Password);
        cancellationToken.ThrowIfCancellationRequested();
        var passwordHash = new PasswordHasher<string>().HashPassword(username, password);
        User? user = null;
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await organizationRepository.AcquireMutationLockAsync(cancellationToken);
                if (input.DefaultPageId.HasValue)
                    await permissionRepository.AcquireCatalogSharedLockAsync(cancellationToken);
                await ValidateOrganizationAsync(input.OrgId, cancellationToken);
                await ValidateDefaultPageAsync(input.DefaultPageId, cancellationToken);
                if (await userRepository.UsernameExistsAsync(username, cancellationToken))
                {
                    throw new UserDomainException(MessageKeys.Users.UsernameExists, DomainErrorType.Conflict);
                }
                user = new User(
                    Guid.CreateVersion7(),
                    username,
                    passwordHash,
                    input.DisplayName,
                    input.Phone,
                    input.Email,
                    input.DataPermissionType,
                    input.DefaultPageId
                )
                {
                    Enable = input.Enable,
                    OrgId = input.OrgId,
                };
                await userRepository.AddAsync(user, cancellationToken);
            },
            cancellationToken
        );
        return user!.Adapt<UserListOutput>();
    }

    public async Task UpdateAsync(Guid id, UpdateUserInput input, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await organizationRepository.AcquireMutationLockAsync(cancellationToken);
                if (input.DefaultPageId.HasValue)
                    await permissionRepository.AcquireCatalogSharedLockAsync(cancellationToken);
                var user = await GetRequiredAsync(id, cancellationToken);
                await ValidateOrganizationAsync(input.OrgId, cancellationToken);
                await ValidateDefaultPageAsync(input.DefaultPageId, cancellationToken);
                user.UpdateProfile(
                    input.DisplayName,
                    input.Phone,
                    input.Email,
                    input.DataPermissionType,
                    input.DefaultPageId,
                    input.Version,
                    SystemIds.System,
                    DateTimeExtension.Now()
                );
                user.OrgId = input.OrgId;
                await userRepository.SaveAsync(user, cancellationToken);
                await userInfoCacheInvalidationPublisher.PublishAsync(id, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task SetEnableAsync(Guid id, SetUserEnableInput input, CancellationToken cancellationToken)
    {
        // 规范：禁用或删除用户时，令牌失效标记必须在数据库事务提交前成功写入；即使后续回滚会造成额外登出，
        // 也不得改为提交后异步失效，否则会产生用户已禁用或删除但旧令牌仍可用的安全窗口。
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await userRepository.AcquireSecurityLockAsync(id, cancellationToken);
                var user = await GetRequiredAsync(id, cancellationToken);
                user.SetEnable(input.Enable, input.Version, SystemIds.System, DateTimeExtension.Now());
                await userRepository.SaveAsync(user, cancellationToken);
                if (!input.Enable && !await userTokenCacheService.SetInvalidBeforeAsync(id, cancellationToken))
                {
                    throw new InvalidOperationException("设置用户令牌失效时间失败");
                }
                await userInfoCacheInvalidationPublisher.PublishAsync(id, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task<ResetPasswordOutput> ResetPasswordAsync(
        Guid id,
        Guid version,
        CancellationToken cancellationToken
    )
    {
        var password = GeneratePassword();
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await userRepository.AcquireSecurityLockAsync(id, cancellationToken);
                var user = await GetRequiredAsync(id, cancellationToken);
                var passwordHash = new PasswordHasher<string>().HashPassword(user.Username, password);
                user.ResetPassword(passwordHash, version, SystemIds.System, DateTimeExtension.Now());
                await userRepository.SavePasswordAsync(user, cancellationToken);
                // 密码变更与禁用、删除具有相同的会话安全要求，失效标记必须在事务提交前写入。
                // 失效时间与 JWT iat 均为秒级；重置后同一秒立即登录时，新令牌可能被判为失效。
                // 该极短边界属于当前方案的已知取舍，不额外等待或引入安全版本号处理。
                if (!await userTokenCacheService.SetInvalidBeforeAsync(id, cancellationToken))
                    throw new InvalidOperationException("设置用户令牌失效时间失败");
            },
            cancellationToken
        );
        return new ResetPasswordOutput(password);
    }

    public async Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await userRepository.AcquireSecurityLockAsync(id, cancellationToken);
                var user = await GetRequiredAsync(id, cancellationToken);
                user.EnsureCanDelete(version);
                await userRepository.DeleteAsync(id, version, cancellationToken);
                if (!await userTokenCacheService.SetInvalidBeforeAsync(id, cancellationToken))
                {
                    throw new InvalidOperationException("设置用户令牌失效时间失败");
                }
                await userInfoCacheInvalidationPublisher.PublishAsync(id, cancellationToken);
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<UserOutput> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user =
            await userInfoCacheService.FindAsync(id, cancellationToken)
            ?? throw new UserDomainException(MessageKeys.Users.NotFound, DomainErrorType.NotFound);
        var defaultPagePath = "";
        // ReSharper disable once InvertIf
        if (user.DefaultPageId.HasValue)
        {
            // 权限可能在用户配置后被禁用或失去有效祖先，读取时必须按当前权限树重新判断。
            var permission = (await permissionRepository.GetAllAsync(cancellationToken)).SingleOrDefault(candidate =>
                candidate.Id == user.DefaultPageId.Value
            );
            if (permission is { Type: PermissionType.Page })
                defaultPagePath = permission.RoutePath;
        }

        return new UserOutput(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Phone,
            user.Email,
            defaultPagePath,
            user.DataPermissionType
        );
    }

    /// <inheritdoc />
    public async Task<LoginOutput> LoginAsync(LoginInput request, CancellationToken cancellationToken)
    {
        var candidate = await userRepository.GetByAccountAsync(request.Account, cancellationToken);
        if (candidate == null)
        {
            throw new UserDomainException(MessageKeys.Users.CredentialsInvalid);
        }

        var valid = new PasswordHasher<string>().VerifyHashedPassword(
            request.Account,
            candidate.PasswordHash,
            request.Password
        );
        if (valid.Equals(PasswordVerificationResult.Failed))
        {
            throw new UserDomainException(MessageKeys.Users.CredentialsInvalid);
        }

        TokenPair? tokens = null;
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await userRepository.AcquireSecurityLockAsync(candidate.Id, cancellationToken);
                // 等待锁期间用户状态可能已发生变化，必须在锁内重新读取。
                var user = await userRepository.GetAsync(candidate.Id, cancellationToken);
                if (user == null || !string.Equals(user.PasswordHash, candidate.PasswordHash, StringComparison.Ordinal))
                {
                    throw new UserDomainException(MessageKeys.Users.CredentialsInvalid);
                }

                if (!user.Enable)
                {
                    throw new UserDomainException(MessageKeys.Users.Disabled);
                }

                var sessionId = Guid.CreateVersion7();
                tokens = tokenService.Generate(user, sessionId);
                if (!await userTokenCacheService.SaveAsync(user.Id, sessionId, tokens, cancellationToken))
                {
                    throw new InvalidOperationException("保存登录会话失败");
                }
            },
            cancellationToken
        );
        return tokens!.Adapt<LoginOutput>();
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

    private static string GeneratePassword()
    {
        var characterGroups = new[] { PasswordUppercase, PasswordLowercase, PasswordDigits, PasswordSpecial };
        var allCharacters = string.Concat(characterGroups);
        var password = new char[16];
        for (var index = 0; index < characterGroups.Length; index++)
        {
            var group = characterGroups[index];
            password[index] = group[RandomNumberGenerator.GetInt32(group.Length)];
        }
        for (var index = characterGroups.Length; index < password.Length; index++)
            password[index] = allCharacters[RandomNumberGenerator.GetInt32(allCharacters.Length)];

        // 打散强制字符的位置，避免密码结构泄露固定模式。
        for (var index = password.Length - 1; index > 0; index--)
        {
            var target = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[target]) = (password[target], password[index]);
        }
        return new string(password);
    }

    private async Task ValidateOrganizationAsync(Guid orgId, CancellationToken cancellationToken)
    {
        if (orgId == Guid.Empty)
            throw new UserDomainException(MessageKeys.Users.OrgIdRequired);
        var organization =
            await organizationRepository.FindAsync(orgId, cancellationToken)
            ?? throw new UserDomainException(MessageKeys.Users.OrganizationNotFound, DomainErrorType.NotFound);
        if (!organization.Enable)
            throw new UserDomainException(MessageKeys.Users.OrganizationDisabled);
    }

    private async Task ValidateDefaultPageAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id == Guid.Empty)
            return;
        var permission = (await permissionRepository.GetAllAsync(cancellationToken)).SingleOrDefault(candidate =>
            candidate.Id == id.Value
        );
        if (permission is not { Type: PermissionType.Page })
            throw new UserDomainException(MessageKeys.Users.DefaultPageInvalid);
    }
}
