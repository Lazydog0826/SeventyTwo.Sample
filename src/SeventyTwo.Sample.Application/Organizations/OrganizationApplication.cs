using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Organizations;

namespace SeventyTwo.Sample.Application.Organizations;

[AutofacDependency(typeof(IOrganizationApplication))]
public sealed class OrganizationApplication(IOrganizationRepository organizationRepository, IUnitOfWork unitOfWork)
    : IOrganizationApplication
{
    public async Task<OrganizationListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken) =>
        ToListOutput(await GetRequiredAsync(id, cancellationToken));

    public async Task<OrganizationListOutput> CreateAsync(
        CreateOrganizationInput input,
        CancellationToken cancellationToken
    )
    {
        var id = Guid.CreateVersion7();
        ValidateParentReference(id, input.ParentId);
        Organization? organization = null;
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await organizationRepository.AcquireMutationLockAsync(cancellationToken);
                var parent = input.ParentId is null
                    ? null
                    : await GetRequiredAsync(input.ParentId.Value, cancellationToken);
                var orgId = parent?.OrgId ?? id;
                organization = new Organization(
                    id,
                    input.Code,
                    input.Name,
                    input.ParentId,
                    parent is null ? null : $"{parent.Path}/{id}",
                    input.SortOrder
                )
                {
                    Enable = input.Enable,
                    OrgId = orgId,
                };
                await ValidateCodeAsync(orgId, organization.Code, null, cancellationToken);
                await organizationRepository.AddAsync(organization, cancellationToken);
            },
            cancellationToken
        );
        return ToListOutput(organization!);
    }

    public async Task UpdateAsync(Guid id, UpdateOrganizationInput input, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.IdRequired);
        }

        ValidateParentReference(id, input.ParentId);
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await organizationRepository.AcquireMutationLockAsync(cancellationToken);
                var organization = await GetRequiredAsync(id, cancellationToken);
                var parent = await ValidateParentChangeAsync(organization, input.ParentId, cancellationToken);
                var normalizedCode = string.IsNullOrWhiteSpace(input.Code)
                    ? throw new OrganizationDomainException(MessageKeys.Organizations.CodeRequired)
                    : input.Code.Trim();
                await ValidateCodeAsync(organization.OrgId, normalizedCode, id, cancellationToken);
                organization.Update(
                    input.Code,
                    input.Name,
                    input.Enable,
                    input.ParentId,
                    input.Version,
                    SystemIds.System,
                    DateTimeExtension.Now(),
                    input.SortOrder
                );
                if (parent is not null)
                {
                    organization.ChangePath(parent.Path);
                }
                await organizationRepository.SaveAsync(organization, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await organizationRepository.AcquireMutationLockAsync(cancellationToken);
                _ = await GetRequiredAsync(id, cancellationToken);
                await organizationRepository.DeleteAsync(id, cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<OrganizationListOutput>> GetListAsync(CancellationToken cancellationToken)
    {
        var organizations = await organizationRepository.GetListAsync(cancellationToken);
        return organizations.Select(ToListOutput).ToList();
    }

    private async Task<Organization> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.IdRequired);
        }

        return await organizationRepository.FindAsync(id, cancellationToken)
            ?? throw new OrganizationDomainException(MessageKeys.Organizations.NotFound, DomainErrorType.NotFound);
    }

    private async Task ValidateCodeAsync(Guid orgId, string code, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await organizationRepository.CodeExistsAsync(orgId, code, excludedId, cancellationToken))
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.CodeExists, DomainErrorType.Conflict);
        }
    }

    private async Task<Organization?> ValidateParentChangeAsync(
        Organization organization,
        Guid? parentId,
        CancellationToken cancellationToken
    )
    {
        if (organization.ParentId is null && parentId is not null)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.RootCannotBeChild);
        }
        if (organization.ParentId is not null && parentId is null)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ChildCannotBeRoot);
        }
        if (parentId is null)
        {
            return null;
        }

        var organizations = await organizationRepository.GetListAsync(cancellationToken);
        var byId = organizations.ToDictionary(item => item.Id);
        if (!byId.TryGetValue(parentId.Value, out var parent))
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ParentNotFound, DomainErrorType.NotFound);
        }
        if (parent.OrgId != organization.OrgId)
        {
            throw new OrganizationDomainException(
                MessageKeys.Organizations.CrossRootMoveNotAllowed,
                DomainErrorType.Conflict
            );
        }

        var currentId = parentId;
        var visited = new HashSet<Guid>();
        while (currentId is not null)
        {
            if (!visited.Add(currentId.Value))
            {
                throw new OrganizationDomainException(MessageKeys.Organizations.HierarchyCycle);
            }
            if (currentId == organization.Id)
            {
                throw new OrganizationDomainException(MessageKeys.Organizations.DescendantCannotBeParent);
            }
            currentId = byId.TryGetValue(currentId.Value, out var current) ? current.ParentId : null;
        }
        return parent;
    }

    private static void ValidateParentReference(Guid id, Guid? parentId)
    {
        if (parentId == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.ParentIdRequired);
        }

        if (parentId == id)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.SelfCannotBeParent);
        }
    }

    private static OrganizationListOutput ToListOutput(Organization organization) =>
        new()
        {
            Id = organization.Id,
            Code = organization.Code,
            Name = organization.Name,
            Enable = organization.Enable,
            ParentId = organization.ParentId,
            SortOrder = organization.SortOrder,
            Version = organization.Version,
        };
}
