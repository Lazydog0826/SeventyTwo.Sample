namespace SeventyTwo.Sample.Application.Products;

public sealed record CreateProductInput(string Name, decimal Price);

public sealed record UpdateProductInput(string Name, decimal Price, long Version);

public sealed record ProductOutput
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public long Version { get; init; }
}
