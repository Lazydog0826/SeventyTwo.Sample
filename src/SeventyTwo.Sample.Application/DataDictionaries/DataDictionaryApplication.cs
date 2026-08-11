using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.DataDictionaries;

namespace SeventyTwo.Sample.Application.DataDictionaries;

[AutofacDependency(typeof(IDataDictionaryApplication))]
public sealed class DataDictionaryApplication(IDataDictionaryRepository repository, IUnitOfWork unitOfWork)
    : IDataDictionaryApplication
{
    public async Task<DataDictionaryListOutput> CreateAsync(
        CreateDataDictionaryInput input,
        CancellationToken cancellationToken
    )
    {
        var dictionary = new DataDictionary(Guid.CreateVersion7(), input.Code, input.Name, input.Description)
        {
            Enable = input.Enable,
            OrgId = Guid.Empty,
        };
        await ValidateCodeAsync(dictionary.Code, null, cancellationToken);
        await unitOfWork.ExecuteAsync(() => repository.AddAsync(dictionary, cancellationToken), cancellationToken);
        return dictionary.Adapt<DataDictionaryListOutput>();
    }

    public async Task UpdateAsync(Guid id, UpdateDataDictionaryInput input, CancellationToken cancellationToken)
    {
        var dictionary = await GetRequiredAsync(id, cancellationToken);
        var normalizedCode = string.IsNullOrWhiteSpace(input.Code)
            ? throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.CodeRequired)
            : input.Code.Trim();
        await ValidateCodeAsync(normalizedCode, id, cancellationToken);
        dictionary.Update(
            input.Code,
            input.Name,
            input.Description,
            input.Enable,
            input.Version,
            SystemIds.System,
            DateTimeExtension.Now()
        );
        await unitOfWork.ExecuteAsync(() => repository.SaveAsync(dictionary, cancellationToken), cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(id, cancellationToken);
        await unitOfWork.ExecuteAsync(() => repository.DeleteAsync(id, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<DataDictionaryListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var dictionaries = await repository.GetListAsync(cancellationToken);
        return dictionaries.Adapt<List<DataDictionaryListOutput>>();
    }

    public async Task<DataDictionaryItemsOutput> GetItemsAsync(Guid id, CancellationToken cancellationToken) =>
        (await GetRequiredAsync(id, cancellationToken)).Adapt<DataDictionaryItemsOutput>();

    public async Task<DataDictionaryItemMutationOutput> CreateItemAsync(
        CreateDataDictionaryItemInput input,
        CancellationToken cancellationToken
    )
    {
        var dictionary = await GetRequiredAsync(input.DictionaryId, cancellationToken);
        var item = dictionary.AddItem(
            Guid.CreateVersion7(),
            input.Value,
            input.Label,
            input.SortOrder,
            input.DictionaryVersion,
            SystemIds.System,
            DateTimeExtension.Now()
        );
        await unitOfWork.ExecuteAsync(
            () => repository.SaveItemsAsync(dictionary, cancellationToken),
            cancellationToken
        );
        return new DataDictionaryItemMutationOutput(dictionary.Version, item.Adapt<DataDictionaryItemOutput>());
    }

    public async Task<DataDictionaryItemMutationOutput> UpdateItemAsync(
        UpdateDataDictionaryItemInput input,
        CancellationToken cancellationToken
    )
    {
        var dictionary = await GetRequiredAsync(input.DictionaryId, cancellationToken);
        dictionary.UpdateItem(
            input.Id,
            input.Value,
            input.Label,
            input.SortOrder,
            input.DictionaryVersion,
            SystemIds.System,
            DateTimeExtension.Now()
        );
        await unitOfWork.ExecuteAsync(
            () => repository.SaveItemsAsync(dictionary, cancellationToken),
            cancellationToken
        );
        return new DataDictionaryItemMutationOutput(
            dictionary.Version,
            dictionary.Items.Single(item => item.Id == input.Id).Adapt<DataDictionaryItemOutput>()
        );
    }

    public async Task<DataDictionaryItemMutationOutput> DeleteItemAsync(
        DeleteDataDictionaryItemInput input,
        CancellationToken cancellationToken
    )
    {
        var dictionary = await GetRequiredAsync(input.DictionaryId, cancellationToken);
        dictionary.RemoveItem(input.Id, input.DictionaryVersion, SystemIds.System, DateTimeExtension.Now());
        await unitOfWork.ExecuteAsync(
            () => repository.SaveItemsAsync(dictionary, cancellationToken),
            cancellationToken
        );
        return new DataDictionaryItemMutationOutput(dictionary.Version, null);
    }

    public async Task<IReadOnlyList<DataDictionaryOptionOutput>> GetOptionsByCodeAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.CodeRequired);
        }

        var dictionary =
            await repository.FindEnabledByCodeAsync(code.Trim(), cancellationToken)
            ?? throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.NotFound, DomainErrorType.NotFound);
        return dictionary
            .Items.OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Adapt<List<DataDictionaryOptionOutput>>();
    }

    private async Task<DataDictionary> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.IdRequired);
        }

        return await repository.FindAsync(id, cancellationToken)
            ?? throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.NotFound, DomainErrorType.NotFound);
    }

    private async Task ValidateCodeAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await repository.CodeExistsAsync(code, excludedId, cancellationToken))
        {
            throw new DataDictionaryDomainException(MessageKeys.DataDictionaries.CodeExists, DomainErrorType.Conflict);
        }
    }
}
