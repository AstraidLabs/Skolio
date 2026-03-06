using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Domain.Entities;

public sealed class ParentStudentLink
{
    private ParentStudentLink(Guid id, Guid parentUserProfileId, Guid studentUserProfileId, string relationship)
    {
        Id = id;
        ParentUserProfileId = parentUserProfileId;
        StudentUserProfileId = studentUserProfileId;
        Relationship = relationship.Trim();
    }

    public Guid Id { get; }
    public Guid ParentUserProfileId { get; }
    public Guid StudentUserProfileId { get; }
    public string Relationship { get; }

    public static ParentStudentLink Create(Guid id, Guid parentUserProfileId, Guid studentUserProfileId, string relationship)
    {
        if (id == Guid.Empty || parentUserProfileId == Guid.Empty || studentUserProfileId == Guid.Empty)
            throw new IdentityDomainException("Parent-student link ids are required.");
        if (parentUserProfileId == studentUserProfileId)
            throw new IdentityDomainException("Parent and student profiles must be different users.");
        if (string.IsNullOrWhiteSpace(relationship))
            throw new IdentityDomainException("Parent-student relationship is required.");

        return new ParentStudentLink(id, parentUserProfileId, studentUserProfileId, relationship);
    }
}
