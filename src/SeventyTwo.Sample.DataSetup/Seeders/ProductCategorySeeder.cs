using SeventyTwo.Sample.Infrastructure.Products;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 类目种子结果：仅暴露被商品种子引用的叶子类目 Id，其余类目无下游引用。
internal sealed record ProductCategorySeedResult(
    Guid SmartphoneId,
    Guid LaptopId,
    Guid WearableId,
    Guid RefrigeratorId,
    Guid WashingMachineId,
    Guid MenswearId,
    Guid WomenswearId,
    Guid SnackId,
    Guid GrainOilId
);

// 商品类目种子：构建测试类目树。
internal static class ProductCategorySeeder
{
    public static ProductCategorySeedResult Seed(SqlSugarClient db)
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

        // 测试类目树最深三级，便于验证类目树展示与上级类目选择；同级按声明顺序分配排序号。
        var digital = CreateCategory(digitalId, "电子数码", null, null, 1);
        var phone = CreateCategory(phoneId, "手机通讯", digitalId, digital.Path, 1);
        var smartphone = CreateCategory(smartphoneId, "智能手机", phoneId, phone.Path, 1);
        var featurePhone = CreateCategory(featurePhoneId, "功能手机", phoneId, phone.Path, 2);
        var computer = CreateCategory(computerId, "电脑办公", digitalId, digital.Path, 2);
        var laptop = CreateCategory(laptopId, "笔记本电脑", computerId, computer.Path, 1);
        var desktop = CreateCategory(desktopId, "台式电脑", computerId, computer.Path, 2);
        var wearable = CreateCategory(wearableId, "智能穿戴", digitalId, digital.Path, 3);
        var appliance = CreateCategory(applianceId, "家用电器", null, null, 2);
        var majorAppliance = CreateCategory(majorApplianceId, "大家电", applianceId, appliance.Path, 1);
        var refrigerator = CreateCategory(refrigeratorId, "冰箱", majorApplianceId, majorAppliance.Path, 1);
        var washingMachine = CreateCategory(washingMachineId, "洗衣机", majorApplianceId, majorAppliance.Path, 2);
        var kitchenAppliance = CreateCategory(kitchenApplianceId, "厨房电器", applianceId, appliance.Path, 2);
        var apparel = CreateCategory(apparelId, "服装鞋帽", null, null, 3);
        var menswear = CreateCategory(menswearId, "男装", apparelId, apparel.Path, 1);
        var womenswear = CreateCategory(womenswearId, "女装", apparelId, apparel.Path, 2);
        var food = CreateCategory(foodId, "食品生鲜", null, null, 4);
        var snack = CreateCategory(snackId, "休闲零食", foodId, food.Path, 1);
        var grainOil = CreateCategory(grainOilId, "粮油调味", foodId, food.Path, 2);
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

        return new ProductCategorySeedResult(
            smartphoneId,
            laptopId,
            wearableId,
            refrigeratorId,
            washingMachineId,
            menswearId,
            womenswearId,
            snackId,
            grainOilId
        );
    }

    private static ProductCategoryRecord CreateCategory(
        Guid id,
        string name,
        Guid? parentId,
        string? parentPath,
        int sortOrder
    ) =>
        new()
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            SortOrder = sortOrder,
            Path = parentPath is null ? id.ToString() : $"{parentPath}/{id}",
        };
}
