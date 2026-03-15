namespace Skolio.Academics.Application.Abstractions;

public interface ICurrentUserContext
{
    string UserId { get; }
    bool IsPlatformAdministrator { get; }
    bool HasAccessToSchool(Guid schoolId);
}
