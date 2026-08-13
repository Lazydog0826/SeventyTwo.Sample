using System.Data.Common;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.DataDictionaries;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.DataDictionaries;
using SeventyTwo.Sample.Infrastructure.DataDictionaries;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class DataDictionaryCrudTests
{
    [Theory]
    [InlineData(nameof(DataDictionariesController.GetListAsync), "list", "dataDictionariesList")]
    [InlineData(nameof(DataDictionariesController.GetItemsAsync), "{id:guid}/items", "dataDictionariesList")]
    [InlineData(nameof(DataDictionariesController.CreateAsync), "create", "dataDictionariesCreate")]
    [InlineData(nameof(DataDictionariesController.UpdateAsync), "update", "dataDictionariesUpdate")]
    [InlineData(nameof(DataDictionariesController.DeleteAsync), "delete", "dataDictionariesDelete")]
    [InlineData(nameof(DataDictionariesController.CreateItemAsync), "items/create", "dataDictionariesUpdate")]
    [InlineData(nameof(DataDictionariesController.UpdateItemAsync), "items/update", "dataDictionariesUpdate")]
    [InlineData(nameof(DataDictionariesController.DeleteItemAsync), "items/delete", "dataDictionariesUpdate")]
    public async Task ManagementEndpoints_ShouldUseExpectedPermission(string methodName, string route, string code)
    {
        var method = typeof(DataDictionariesController).GetMethod(methodName)!;
        Assert.Equal(route, method.GetCustomAttribute<HttpMethodAttribute>()!.Template);
        var permission = method.GetCustomAttribute<PermissionAttribute>()!;
        var policy = await new PermissionPolicyProvider(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Authorization.AuthorizationOptions())
        ).GetPolicyAsync(permission.Policy!);
        Assert.Equal([code], Assert.Single(policy!.Requirements.OfType<PermissionRequirement>()).PermissionCodes);
    }

    [Fact]
    public void LookupEndpoint_ShouldRelyOnAuthenticatedFallbackOnly()
    {
        var method = typeof(DataDictionariesController).GetMethod(nameof(DataDictionariesController.GetOptionsByCodeAsync))!;

        Assert.Null(method.GetCustomAttribute<PermissionAttribute>());
        Assert.Equal("by-code/{code}/items", method.GetCustomAttribute<HttpMethodAttribute>()!.Template);
    }

    [Fact]
    public async Task Application_ShouldRejectDuplicateCodeAndReturnEnabledOptions()
    {
        var enabled = CreateDictionary("ENABLED", true);
        enabled.AddItem(Guid.CreateVersion7(), "B", "乙", 2, enabled.Version, Guid.Empty, DateTimeOffset.UtcNow);
        enabled.AddItem(Guid.CreateVersion7(), "A", "甲", 1, enabled.Version, Guid.Empty, DateTimeOffset.UtcNow);
        var repository = new FakeRepository([enabled]);
        var application = new DataDictionaryApplication(repository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<DataDictionaryDomainException>(() =>
            application.CreateAsync(new("ENABLED", "重复", null, true), CancellationToken.None)
        );
        var options = await application.GetOptionsByCodeAsync("ENABLED", CancellationToken.None);

        Assert.Equal(["A", "B"], options.Select(option => option.Value));
    }

    [Fact]
    public async Task Repository_ShouldAdvanceVersionSortItemsAndCascadeDelete()
    {
        var path = Path.Combine(Path.GetTempPath(), $"data-dictionary-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<DataDictionaryRecord, DataDictionaryItemRecord>();
            var id = Guid.CreateVersion7();
            var version = Guid.CreateVersion7();
            await db.Insertable(new DataDictionaryRecord { Id = id, Code = "STATUS", Name = "状态", OrgId = Guid.Empty, Version = version }).ExecuteCommandAsync();
            await db.Insertable(new[]
            {
                new DataDictionaryItemRecord { Id = Guid.CreateVersion7(), DictionaryId = id, Value = "B", Label = "乙", SortOrder = 2 },
                new DataDictionaryItemRecord { Id = Guid.CreateVersion7(), DictionaryId = id, Value = "A", Label = "甲", SortOrder = 1 },
            }).ExecuteCommandAsync();
            var repository = new DataDictionaryRepository(db);
            var dictionary = Assert.IsType<DataDictionary>(await repository.FindEnabledByCodeAsync("STATUS", CancellationToken.None));

            Assert.Equal(["A", "B"], dictionary.Items.Select(item => item.Value));
            dictionary.UpdateItem(dictionary.Items[0].Id, "A1", "甲一", 1, dictionary.Version, Guid.Empty, DateTimeOffset.UtcNow);
            await repository.SaveItemsAsync(dictionary, CancellationToken.None);
            Assert.NotEqual(version, dictionary.Version);

            await repository.DeleteAsync(id, CancellationToken.None);
            Assert.Equal(0, await db.Queryable<DataDictionaryRecord>().CountAsync());
            Assert.Equal(0, await db.Queryable<DataDictionaryItemRecord>().CountAsync());
            db.Ado.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RepositoryGetPage_ShouldFilterCountAndLoadCurrentPageItemCounts()
    {
        var path = Path.Combine(Path.GetTempPath(), $"data-dictionary-page-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<DataDictionaryRecord, DataDictionaryItemRecord>();
            var firstId = Guid.CreateVersion7();
            var secondId = Guid.CreateVersion7();
            await db.Insertable(new[]
            {
                new DataDictionaryRecord { Id = firstId, Code = "STATUS", Name = "Status", Enable = true, OrgId = Guid.Empty },
                new DataDictionaryRecord { Id = secondId, Code = "OTHER", Name = "Other", Enable = false, OrgId = Guid.Empty },
            }).ExecuteCommandAsync();
            await db.Insertable(new[]
            {
                new DataDictionaryItemRecord { Id = Guid.CreateVersion7(), DictionaryId = firstId, Value = "1", Label = "One" },
                new DataDictionaryItemRecord { Id = Guid.CreateVersion7(), DictionaryId = firstId, Value = "2", Label = "Two" },
                new DataDictionaryItemRecord { Id = Guid.CreateVersion7(), DictionaryId = secondId, Value = "3", Label = "Three" },
            }).ExecuteCommandAsync();

            var page = await new DataDictionaryRepository(db).GetPageAsync(
                new DataDictionaryPageRequest { Index = 1, Limit = 1, Keyword = " status ", Enable = true },
                CancellationToken.None
            );

            Assert.Equal(1, page.Total);
            Assert.Equal(2, Assert.Single(page.Items).Items.Count);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0, 20, MessageKeys.Paging.PageNumberMustBePositive)]
    [InlineData(1, 0, MessageKeys.Paging.PageSizeOutOfRange100)]
    [InlineData(1, 101, MessageKeys.Paging.PageSizeOutOfRange100)]
    [InlineData(int.MaxValue, 100, MessageKeys.Paging.PageOffsetOutOfRange)]
    public async Task ApplicationGetPage_ShouldValidatePaging(int index, int limit, string message)
    {
        var application = new DataDictionaryApplication(new FakeRepository([]), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<DataDictionaryDomainException>(() =>
            application.GetPageAsync(
                new DataDictionaryPageRequest { Index = index, Limit = limit },
                CancellationToken.None
            )
        );

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Repository_ShouldRecognizeWrappedPostgreSqlUniqueViolation()
    {
        var exception = new InvalidOperationException("wrapper", new TestDbException("23505"));

        Assert.True(DataDictionaryRepository.IsCodeConflict(exception));
        Assert.False(DataDictionaryRepository.IsCodeConflict(new TestDbException("40001")));
    }

    private static DataDictionary CreateDictionary(string code, bool enable, IEnumerable<DataDictionaryItem>? items = null)
    {
        var dictionary = new DataDictionary(Guid.CreateVersion7(), code, code, null, items)
        {
            Enable = enable,
            Version = Guid.CreateVersion7(),
        };
        return dictionary;
    }

    private static SqlSugarClient CreateDatabase(string path) =>
        new(new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = $"Data Source={path};Pooling=False", IsAutoCloseConnection = true });

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken) => action();
    }

    private sealed class TestDbException(string sqlState) : DbException
    {
        public override string? SqlState => sqlState;
    }

    private sealed class FakeRepository(IEnumerable<DataDictionary> dictionaries) : IDataDictionaryRepository
    {
        private readonly List<DataDictionary> items = [.. dictionaries];
        public Task<DataDictionary?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(items.SingleOrDefault(item => item.Id == id));
        public Task<DataDictionary?> FindEnabledByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(items.SingleOrDefault(item => item.Enable && item.Code == code));
        public Task<DataDictionaryPage> GetPageAsync(DataDictionaryPageRequest request, CancellationToken cancellationToken) => Task.FromResult(new DataDictionaryPage(items, items.Count));
        public Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken) => Task.FromResult(items.Any(item => item.Code == code && item.Id != excludedId));
        public Task AddAsync(DataDictionary dictionary, CancellationToken cancellationToken) { items.Add(dictionary); return Task.CompletedTask; }
        public Task SaveAsync(DataDictionary dictionary, CancellationToken cancellationToken) { dictionary.Version = Guid.CreateVersion7(); return Task.CompletedTask; }
        public Task SaveItemsAsync(DataDictionary dictionary, CancellationToken cancellationToken) { dictionary.Version = Guid.CreateVersion7(); return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) { items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
    }
}
