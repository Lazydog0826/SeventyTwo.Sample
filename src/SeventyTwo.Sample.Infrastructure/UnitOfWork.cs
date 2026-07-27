using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure;

[AutofacDependency(typeof(IUnitOfWork))]
public sealed class UnitOfWork(ISqlSugarClient db) : IUnitOfWork
{
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = db.CreateContext(db.Ado.IsNoTran());
        await action();
        context.Commit();
    }
}
