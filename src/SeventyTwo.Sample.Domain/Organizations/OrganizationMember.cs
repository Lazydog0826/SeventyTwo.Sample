// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Organizations;

public sealed class OrganizationMember : AggregateRoot
{
    private OrganizationMember() { }

    public OrganizationMember(Guid id, Guid organizationId, Guid userId, bool isPrimary)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.MemberIdRequired);
        }

        if (organizationId == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Organizations.IdRequired);
        }

        if (userId == Guid.Empty)
        {
            throw new OrganizationDomainException(MessageKeys.Users.IdRequired);
        }

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        IsPrimary = isPrimary;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsPrimary { get; private set; }
}
