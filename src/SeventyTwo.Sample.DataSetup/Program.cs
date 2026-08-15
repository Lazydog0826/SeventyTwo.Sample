using Microsoft.Extensions.Configuration;
using SeventyTwo.Sample.DataSetup.Seeders;
using SeventyTwo.Sample.Infrastructure;
using SqlSugar;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();
var connectionString = configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("未配置 ConnectionStrings:PostgreSQL。");
}

using var db = new SqlSugarClient(
    new ConnectionConfig
    {
        DbType = DbType.PostgreSQL,
        IsAutoCloseConnection = true,
        ConnectionString = connectionString,
    }
);

var entityTypes = typeof(AssemblyMarker)
    .Assembly.GetTypes()
    .Where(type => !type.IsAbstract && type.IsDefined(typeof(SugarTable), false))
    .ToArray();

db.Ado.BeginTran();
try
{
    db.CodeFirst.InitTables(entityTypes);
    Console.WriteLine($"已根据 {entityTypes.Length} 个数据库实体完成建表。");

    // 权限种子先行，首页权限 Id 供用户默认页面与用户权限关联使用。
    var homePermissionId = PermissionSeeder.Seed(db);
    var organizations = OrganizationSeeder.Seed(db);
    UserSeeder.Seed(db, homePermissionId, organizations);
    ProductCategorySeeder.Seed(db);

    db.Ado.CommitTran();
    Console.WriteLine("超级管理员、测试机构、测试用户、权限和商品类目初始化完成。");
}
catch
{
    db.Ado.RollbackTran();
    throw;
}
