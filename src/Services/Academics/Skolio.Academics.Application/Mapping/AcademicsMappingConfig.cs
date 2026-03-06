using Mapster;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Mapping;

public sealed class AcademicsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TimetableEntry, TimetableEntryContract>();
        config.NewConfig<LessonRecord, LessonRecordContract>();
        config.NewConfig<AttendanceRecord, AttendanceRecordContract>();
        config.NewConfig<ExcuseNote, ExcuseNoteContract>();
        config.NewConfig<GradeEntry, GradeEntryContract>();
        config.NewConfig<HomeworkAssignment, HomeworkAssignmentContract>();
        config.NewConfig<DailyReport, DailyReportContract>();
    }
}
