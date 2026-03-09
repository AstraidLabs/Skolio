using Mapster;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.Mapping;

public sealed class OrganizationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Skolio.Organization.Domain.ValueObjects.Address, AddressContract>();
        config.NewConfig<SchoolOperator, SchoolOperatorContract>();
        config.NewConfig<Founder, FounderContract>();
        config.NewConfig<School, SchoolContract>();
        config.NewConfig<SchoolYear, SchoolYearContract>()
            .Map(dest => dest.StartDate, src => src.Period.StartDate)
            .Map(dest => dest.EndDate, src => src.Period.EndDate);
        config.NewConfig<ClassRoom, ClassRoomContract>();
        config.NewConfig<TeachingGroup, TeachingGroupContract>();
        config.NewConfig<Subject, SubjectContract>();
        config.NewConfig<TeacherAssignment, TeacherAssignmentContract>();
    }
}
