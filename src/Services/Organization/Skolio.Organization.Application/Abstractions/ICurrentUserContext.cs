namespace Skolio.Organization.Application.Abstractions;

public interface ICurrentUserContext
{
    string UserId { get; }
    bool IsPlatformAdministrator { get; }
    bool HasAccessToSchool(Guid schoolId);
}
