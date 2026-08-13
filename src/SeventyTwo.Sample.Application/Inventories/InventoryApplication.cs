using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Application.Inventories.StorageFeeCalc;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Application.Inventories;

[AutofacDependency(typeof(IInventoryApplication))]
public sealed class InventoryApplication(IChangeInventoryService changeInventoryService) : IInventoryApplication
{
    public async Task ChangeAsync(ChangeInventoryInput input, CancellationToken cancellationToken)
    {
        var draft = input.Adapt<InventoryChangeDraft>();
        await changeInventoryService.ChangeAsync(draft, cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, decimal>> StorageFeeCalcAsync()
    {
        // 七天免租期
        const int freePeriod = 7;

        // 前一天的模拟库存数据
        var stockAgeList = new List<StockAge>
        {
            new()
            {
                BatchNo = "BATCH-001",
                Days = 5,
                Qty = 100,
            },
            new()
            {
                BatchNo = "BATCH-002",
                Days = 20,
                Qty = 200,
            },
            new()
            {
                BatchNo = "BATCH-003",
                Days = 45,
                Qty = 300,
            },
        };
        var intervals = new List<Interval>
        {
            new()
            {
                MinDays = 0,
                MaxDays = 30,
                Amount = 0.10m,
            },
            new()
            {
                MinDays = 30,
                MaxDays = 60,
                Amount = 0.20m,
            },
        };

        var storageFeeList = new Dictionary<string, decimal>();
        foreach (var stockAge in stockAgeList)
        {
            decimal storageFee = 0;
            if (stockAge.Days > freePeriod)
            {
                var chargeInterval = intervals.FirstOrDefault(x =>
                    stockAge.Days >= x.MinDays && stockAge.Days < x.MaxDays
                );
                if (chargeInterval is not null)
                {
                    storageFee = stockAge.Qty * chargeInterval.Amount;
                }
            }

            storageFeeList.Add(stockAge.BatchNo, storageFee);
        }

        return Task.FromResult<IReadOnlyDictionary<string, decimal>>(storageFeeList);
    }
}
