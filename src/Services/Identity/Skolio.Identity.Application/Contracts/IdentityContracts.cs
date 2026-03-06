using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Application.Contracts;

public sealed record UserProfileContract(Guid Id, string FirstName, string LastName, UserType UserType, string Email);
public sealed record SchoolRoleAssignmentContract(Guid Id, Guid UserProfileId, Guid SchoolId, string RoleCode);
public sealed record ParentStudentLinkContract(Guid Id, Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
