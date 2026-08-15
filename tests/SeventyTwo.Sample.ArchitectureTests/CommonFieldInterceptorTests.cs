using System.Security.Claims;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Persistence;
using SeventyTwo.Sample.Infrastructure.Products;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

/// <summary>
/// 公共字段拦截器行为验证。
/// 不依赖数据库：ToSqlString 生成 SQL 时即触发 DataExecuting 事件，
/// 断言落在实体对象上的自动填充结果（SqlSugar 的 SetValue 会同时改写实体与 SQL 参数）。
/// </summary>
public sealed class CommonFieldInterceptorTests
{
    private readonly Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Guid orgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Insert_ShouldFillCreatedByAndOrgIdFromUserContext()
    {
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId));
        var record = new ProductRecord { Name = "测试商品", Code = "P001" };

        var sql = db.Insertable(record).ToSqlString();

        Assert.Equal(userId, record.CreatedBy);
        Assert.Equal(orgId, record.OrgId);
        Assert.Contains(userId.ToString(), sql);
        Assert.Contains(orgId.ToString(), sql);
    }

    [Fact]
    public void Insert_ShouldKeepExplicitlyAssignedValues()
    {
        var explicitUser = Guid.CreateVersion7();
        var explicitOrg = Guid.CreateVersion7();
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId));
        var record = new ProductRecord
        {
            Name = "测试商品",
            Code = "P001",
            CreatedBy = explicitUser,
            OrgId = explicitOrg,
        };

        db.Insertable(record).ToSqlString();

        Assert.Equal(explicitUser, record.CreatedBy);
        Assert.Equal(explicitOrg, record.OrgId);
    }

    [Fact]
    public void Insert_ShouldFillCreatedAtOverridingPresetValue()
    {
        // 创建时间统一由拦截器在插入时生成（覆盖预置值），调用方预置的历史时间不落库。
        var preset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId));
        var record = new ProductRecord
        {
            Name = "测试商品",
            Code = "P001",
            CreatedAt = preset,
        };

        db.Insertable(record).ToSqlString();

        Assert.NotEqual(preset, record.CreatedAt);
        Assert.True(record.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void UpdateByEntity_ShouldFillUpdatedByAndUpdatedAt()
    {
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId));
        var record = new ProductRecord
        {
            Id = Guid.CreateVersion7(),
            Name = "测试商品",
            Code = "P001",
        };

        var sql = db.Updateable(record)
            .UpdateColumns(x => new
            {
                x.Name,
                x.UpdatedBy,
                x.UpdatedAt,
            })
            .Where(x => x.Id == record.Id)
            .ToSqlString();

        Assert.Equal(userId, record.UpdatedBy);
        Assert.NotNull(record.UpdatedAt);
        Assert.Contains(userId.ToString(), sql);
    }

    [Fact]
    public void Insert_WithUninjectedIdentity_ShouldThrow()
    {
        // 身份未注入属于编程错误：BusinessUserContext 按其设计抛出异常，写入随之失败（fail-fast）。
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId, injectIdentity: false));
        var record = new ProductRecord { Name = "测试商品", Code = "P001" };

        Assert.Throws<InvalidOperationException>(() => db.Insertable(record).ToSqlString());
    }

    [Fact]
    public void UpdateBySetColumns_ShouldNotInjectAuditColumns()
    {
        // 固化已知限制：SetColumns 表达式更新的 SET 列由表达式自身决定，拦截器无法注入修改人；
        // 需要自动填充的更新必须使用实体加 UpdateColumns 风格（见 UpdateByEntity_ShouldFillUpdatedByAndUpdatedAt）。
        using var db = CreateClient(new StubBusinessUserContext(userId, orgId));

        var sql = db.Updateable<ProductRecord>()
            .SetColumns(x => new ProductRecord { Name = "测试商品" })
            .Where(x => x.Id == Guid.CreateVersion7())
            .ToSqlString();

        Assert.DoesNotContain("updated_by", sql);
    }

    private SqlSugarClient CreateClient(IBusinessUserContext userContext)
    {
        var client = new SqlSugarClient(
            new ConnectionConfig
            {
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                // 仅生成 SQL 不建立连接，连接串内容不影响测试。
                ConnectionString = "Host=localhost;Database=ArchitectureTests;Username=test;Password=test",
            }
        );
        CommonFieldInterceptor.Attach(client, userContext);
        return client;
    }

    /// <summary>
    /// 业务用户上下文测试替身：按需注入或保持身份未注入，模拟 HTTP 请求与后台任务两种场景。
    /// </summary>
    private sealed class StubBusinessUserContext(Guid userId, Guid orgId, bool injectIdentity = true)
        : IBusinessUserContext
    {
        public Guid UserId => ThrowIfUninjected(userId);

        public Guid OrgId => ThrowIfUninjected(orgId);

        public DataPermissionType DataPermissionType => ThrowIfUninjected(DataPermissionType.Organization);

        public void FromPrincipal(ClaimsPrincipal user) { }

        public void Set(Guid userId, Guid orgId, DataPermissionType dataPermissionType) { }

        private T ThrowIfUninjected<T>(T value) =>
            injectIdentity ? value : throw new InvalidOperationException("业务用户身份尚未注入。");
    }
}
