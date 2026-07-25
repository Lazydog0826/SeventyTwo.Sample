using Yitter.IdGenerator;
using ApplicationIdGenerator = SeventyTwo.Sample.Application.Abstractions.IIdGenerator;

namespace SeventyTwo.Sample.Infrastructure.Ids;

public sealed class SnowflakeIdGenerator : ApplicationIdGenerator
{
    public long NextId()
    {
        return YitIdHelper.NextId();
    }
}
