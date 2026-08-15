// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Application.Products;

public sealed record CreateProductInput(
    string Name,
    decimal Price,
    string Code,
    string? Description = null,
    string? Unit = null,
    Guid? CategoryId = null,
    ProductStatus Status = ProductStatus.OffShelf
);

public sealed record UpdateProductInput(
    string Name,
    decimal Price,
    string Code,
    ProductStatus Status,
    Guid Version,
    string? Description = null,
    string? Unit = null,
    Guid? CategoryId = null
);

public sealed record ProductOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Code { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Unit { get; init; }

    public Guid? CategoryId { get; init; }

    public ProductStatus Status { get; init; }

    public Guid Version { get; init; }
}
