namespace SeventyTwo.Sample.Application.Products;

public sealed record CreateProductInput(string Name, decimal Price);

public sealed record UpdateProductInput(string Name, decimal Price, string Version);

public sealed record ProductOutput
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Version { get; init; } = string.Empty;
}
