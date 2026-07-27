namespace SeventyTwo.Sample.Application;

public interface IUnitOfWork
{
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken);
}
