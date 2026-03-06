using MediatR;
using Skolio.Identity.Application.Contracts;

namespace Skolio.Identity.Application.ParentStudentLinks;

public sealed record CreateParentStudentLinkCommand(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship) : IRequest<ParentStudentLinkContract>;
