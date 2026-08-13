// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.WebApi.Contracts.Orders;

/// <summary>
/// 随机订单生成请求。
/// </summary>
/// <param name="Count">生成数量。</param>
public sealed record RandomOrdersRequest(int Count);
