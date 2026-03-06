using Mapster;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Application.Mapping;

public sealed class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserProfile, UserProfileContract>();
        config.NewConfig<SchoolRoleAssignment, SchoolRoleAssignmentContract>();
        config.NewConfig<ParentStudentLink, ParentStudentLinkContract>();
    }
}
