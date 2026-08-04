// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application.Products;

public sealed record CreateProductInput(string Name, decimal Price);

public sealed record UpdateProductInput(string Name, decimal Price, Guid Version);

public sealed record ProductOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public Guid Version { get; init; }
}
