using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.Infrastructure.Products;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 商品种子：按类目种子提供的叶子类目初始化测试商品，编码唯一，上架/下架混合便于验证状态筛选。
internal static class ProductSeeder
{
    public static void Seed(SqlSugarClient db, ProductCategorySeedResult categories)
    {
        db.Insertable(
                new[]
                {
                    CreateProduct(
                        "SKU-1001",
                        "旗舰智能手机",
                        4999m,
                        categories.SmartphoneId,
                        "6.7 英寸屏幕，512GB 存储",
                        "台",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-1002",
                        "入门智能手机",
                        1299m,
                        categories.SmartphoneId,
                        "6.5 英寸屏幕，128GB 存储",
                        "台",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-2001",
                        "轻薄笔记本电脑",
                        6499m,
                        categories.LaptopId,
                        "14 英寸，16GB 内存，1TB 固态硬盘",
                        "台",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-2002",
                        "游戏笔记本电脑",
                        8999m,
                        categories.LaptopId,
                        "16 英寸，独立显卡，240Hz 高刷屏",
                        "台",
                        ProductStatus.OffShelf
                    ),
                    CreateProduct(
                        "SKU-3001",
                        "智能手表",
                        1599m,
                        categories.WearableId,
                        "血氧心率监测，14 天长续航",
                        "只",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-4001",
                        "变频双门冰箱",
                        3299m,
                        categories.RefrigeratorId,
                        "302L 容量，一级能效，风冷无霜",
                        "台",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-4002",
                        "滚筒洗衣机",
                        2899m,
                        categories.WashingMachineId,
                        "10KG 洗涤容量，变频电机",
                        "台",
                        ProductStatus.OffShelf
                    ),
                    CreateProduct(
                        "SKU-5001",
                        "男士休闲衬衫",
                        199m,
                        categories.MenswearId,
                        "纯棉面料，修身版型",
                        "件",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-5002",
                        "女士连衣裙",
                        329m,
                        categories.WomenswearId,
                        "碎花雪纺，中长款",
                        "件",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-6001",
                        "混合坚果礼盒",
                        89m,
                        categories.SnackId,
                        "每日坚果 30 包",
                        "盒",
                        ProductStatus.OnShelf
                    ),
                    CreateProduct(
                        "SKU-6002",
                        "有机大米 5kg",
                        65m,
                        categories.GrainOilId,
                        null,
                        "袋",
                        ProductStatus.OffShelf
                    ),
                }
            )
            .ExecuteCommand();
    }

    private static ProductRecord CreateProduct(
        string code,
        string name,
        decimal price,
        Guid? categoryId,
        string? description,
        string? unit,
        ProductStatus status
    ) =>
        new()
        {
            Code = code,
            Name = name,
            Price = price,
            CategoryId = categoryId,
            Description = description,
            Unit = unit,
            Status = status,
        };
}
