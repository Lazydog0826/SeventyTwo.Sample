using System.Data;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure;

/// <summary>
/// 使用同一个数据库事务协调 SqlSugar 业务写入与 CAP 消息持久化。
/// </summary>
/// <remarks>
/// <para>
/// 工作单元采用 Required 传播语义：最外层调用创建事务，内部调用复用该事务，
/// 不创建新事务，也不使用 Savepoint，因此不支持内层事务独立提交或局部回滚。
/// </para>
/// <para>
/// 任一内部调用抛出异常后，当前事务会被标记为 rollback-only。即使业务代码捕获了该异常，
/// 最外层调用仍会回滚整个事务，避免提交已经发生过局部失败的数据。
/// </para>
/// <para>
/// 实例按依赖注入作用域保存嵌套状态，同一实例上的工作单元调用必须顺序执行，
/// 不应使用 <see cref="Task.WhenAll(IEnumerable{Task})" /> 并发共享同一个 SqlSugar 连接和事务。
/// </para>
/// </remarks>
[AutofacDependency(typeof(IUnitOfWork), ServiceLifetime = ServiceLifetime.Scoped)]
public sealed class UnitOfWork(ISqlSugarClient db, ICapPublisher capPublisher) : IUnitOfWork
{
    // 当前逻辑工作单元的调用深度；最外层执行期间为 1，每进入一层嵌套调用递增。
    private int _executionDepth;

    // 内层执行失败后置为 true，确保异常即使被外层 action 捕获，事务仍不能提交。
    private bool _rollbackOnly;

    // 保存首次触发 rollback-only 的异常，作为最终回滚异常的根因。
    private Exception? _rollbackReason;

    /// <summary>
    /// 在业务数据与 CAP 消息共享的数据库事务中执行指定操作。
    /// </summary>
    /// <param name="action">需要纳入事务的业务操作。</param>
    /// <param name="cancellationToken">开始事务、执行操作和提交事务时使用的取消令牌。</param>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 已处于当前工作单元的最外层调用中时，直接加入现有事务；提交权始终归最外层。
        if (_executionDepth > 0)
        {
            await ExecuteNestedAsync(action);
            return;
        }

        // 业务事务只能由当前工作单元创建。拒绝外部预先开启的 SqlSugar 或 CAP 事务，
        // 避免出现多个事务所有者以及提交、回滚、释放职责不明确的问题。
        if (db.Ado.Transaction is not null || capPublisher.Transaction is not null)
        {
            throw new InvalidOperationException("禁止在工作单元外部开启事务，请统一通过 IUnitOfWork 执行");
        }

        // 当前调用拥有即将创建的事务，因此负责提交、回滚、解绑和释放。
        _executionDepth = 1;
        _rollbackOnly = false;
        _rollbackReason = null;
        ICapTransaction? transaction = null;
        try
        {
            // CAP 创建底层数据库事务并保存到 capPublisher.Transaction。
            // autoCommit=false 确保发布消息时只写入 CAP 消息表，统一由本工作单元提交。
            transaction = await db.Ado.Connection.BeginTransactionAsync(
                capPublisher,
                autoCommit: false,
                cancellationToken: cancellationToken
            );

            // 将 CAP 创建的同一个事务实例交给 SqlSugar，后续业务 SQL 和 CAP 消息写入
            // 会使用同一连接、同一 DbTransaction。
            db.Ado.Transaction =
                (IDbTransaction?)transaction.DbTransaction
                ?? throw new InvalidOperationException("CAP 未创建数据库事务");

            try
            {
                await action();
                if (_rollbackOnly)
                {
                    throw new InvalidOperationException("嵌套事务执行失败，外层事务必须回滚", _rollbackReason);
                }
            }
            catch
            {
                // 回滚不能使用已取消的请求令牌，否则客户端取消可能导致回滚操作也被跳过。
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }

            // CAP 先提交底层数据库事务，再将已持久化消息加入发送调度队列。
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // 仅该分支拥有事务。无论提交、回滚或开始事务失败，都必须清除双方引用并释放资源，
            // 防止同一依赖注入作用域的后续操作误用已完成的事务。
            db.Ado.Transaction = null;
            capPublisher.Transaction = null;
            transaction?.Dispose();
            _executionDepth = 0;
            _rollbackOnly = false;
            _rollbackReason = null;
        }
    }

    /// <summary>
    /// 将内部调用加入当前事务，并在失败时把整个逻辑事务标记为只能回滚。
    /// </summary>
    private async Task ExecuteNestedAsync(Func<Task> action)
    {
        _executionDepth++;
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            _rollbackOnly = true;
            _rollbackReason ??= exception;
            throw;
        }
        finally
        {
            _executionDepth--;
        }
    }
}
