// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.WebApi.Contracts.DataDictionaries;

/// <summary>
/// 创建字典请求。
/// </summary>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Enable">是否启用。</param>
public record CreateDataDictionaryRequest(string Code, string Name, string? Description, bool Enable);

/// <summary>
/// 更新字典请求。
/// </summary>
/// <param name="Id">字典 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Enable">是否启用。</param>
/// <param name="Version">并发版本。</param>
public sealed record UpdateDataDictionaryRequest(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool Enable,
    Guid Version
) : CreateDataDictionaryRequest(Code, Name, Description, Enable);

/// <summary>
/// 删除字典请求。
/// </summary>
/// <param name="Id">字典 ID。</param>
public sealed record DeleteDataDictionaryRequest(Guid Id);

/// <summary>
/// 创建字典项请求。
/// </summary>
/// <param name="DictionaryId">字典 ID。</param>
/// <param name="Value">值。</param>
/// <param name="Label">显示文本。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="DictionaryVersion">字典并发版本。</param>
public sealed record CreateDataDictionaryItemRequest(
    Guid DictionaryId,
    string Value,
    string Label,
    int SortOrder,
    Guid DictionaryVersion
);

/// <summary>
/// 更新字典项请求。
/// </summary>
/// <param name="DictionaryId">字典 ID。</param>
/// <param name="Id">字典项 ID。</param>
/// <param name="Value">值。</param>
/// <param name="Label">显示文本。</param>
/// <param name="SortOrder">排序号。</param>
/// <param name="DictionaryVersion">字典并发版本。</param>
public sealed record UpdateDataDictionaryItemRequest(
    Guid DictionaryId,
    Guid Id,
    string Value,
    string Label,
    int SortOrder,
    Guid DictionaryVersion
);

/// <summary>
/// 删除字典项请求。
/// </summary>
/// <param name="DictionaryId">字典 ID。</param>
/// <param name="Id">字典项 ID。</param>
/// <param name="DictionaryVersion">字典并发版本。</param>
public sealed record DeleteDataDictionaryItemRequest(Guid DictionaryId, Guid Id, Guid DictionaryVersion);
