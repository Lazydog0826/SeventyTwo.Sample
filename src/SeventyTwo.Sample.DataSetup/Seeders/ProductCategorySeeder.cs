using SeventyTwo.Sample.Infrastructure.Products;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 商品类目种子：构建测试类目树。
internal static class ProductCategorySeeder
{
    public static void Seed(SqlSugarClient db)
    {
        var digitalId = Guid.CreateVersion7();
        var phoneId = Guid.CreateVersion7();
        var smartphoneId = Guid.CreateVersion7();
        var featurePhoneId = Guid.CreateVersion7();
        var computerId = Guid.CreateVersion7();
        var laptopId = Guid.CreateVersion7();
        var desktopId = Guid.CreateVersion7();
        var wearableId = Guid.CreateVersion7();
        var applianceId = Guid.CreateVersion7();
        var majorApplianceId = Guid.CreateVersion7();
        var refrigeratorId = Guid.CreateVersion7();
        var washingMachineId = Guid.CreateVersion7();
        var kitchenApplianceId = Guid.CreateVersion7();
        var apparelId = Guid.CreateVersion7();
        var menswearId = Guid.CreateVersion7();
        var womenswearId = Guid.CreateVersion7();
        var foodId = Guid.CreateVersion7();
        var snackId = Guid.CreateVersion7();
        var grainOilId = Guid.CreateVersion7();

        // 测试类目树最深三级，便于验证类目树展示与上级类目选择。
        var digital = CreateCategory(digitalId, "电子数码", null, null);
        var phone = CreateCategory(phoneId, "手机通讯", digitalId, digital.Path);
        var smartphone = CreateCategory(smartphoneId, "智能手机", phoneId, phone.Path);
        var featurePhone = CreateCategory(featurePhoneId, "功能手机", phoneId, phone.Path);
        var computer = CreateCategory(computerId, "电脑办公", digitalId, digital.Path);
        var laptop = CreateCategory(laptopId, "笔记本电脑", computerId, computer.Path);
        var desktop = CreateCategory(desktopId, "台式电脑", computerId, computer.Path);
        var wearable = CreateCategory(wearableId, "智能穿戴", digitalId, digital.Path);
        var appliance = CreateCategory(applianceId, "家用电器", null, null);
        var majorAppliance = CreateCategory(majorApplianceId, "大家电", applianceId, appliance.Path);
        var refrigerator = CreateCategory(refrigeratorId, "冰箱", majorApplianceId, majorAppliance.Path);
        var washingMachine = CreateCategory(washingMachineId, "洗衣机", majorApplianceId, majorAppliance.Path);
        var kitchenAppliance = CreateCategory(kitchenApplianceId, "厨房电器", applianceId, appliance.Path);
        var apparel = CreateCategory(apparelId, "服装鞋帽", null, null);
        var menswear = CreateCategory(menswearId, "男装", apparelId, apparel.Path);
        var womenswear = CreateCategory(womenswearId, "女装", apparelId, apparel.Path);
        var food = CreateCategory(foodId, "食品生鲜", null, null);
        var snack = CreateCategory(snackId, "休闲零食", foodId, food.Path);
        var grainOil = CreateCategory(grainOilId, "粮油调味", foodId, food.Path);
        db.Insertable(
                new[]
                {
                    digital,
                    phone,
                    smartphone,
                    featurePhone,
                    computer,
                    laptop,
                    desktop,
                    wearable,
                    appliance,
                    majorAppliance,
                    refrigerator,
                    washingMachine,
                    kitchenAppliance,
                    apparel,
                    menswear,
                    womenswear,
                    food,
                    snack,
                    grainOil,
                }
            )
            .ExecuteCommand();
    }

    private static ProductCategoryRecord CreateCategory(Guid id, string name, Guid? parentId, string? parentPath) =>
        new()
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            Path = parentPath is null ? id.ToString() : $"{parentPath}/{id}",
        };
}
