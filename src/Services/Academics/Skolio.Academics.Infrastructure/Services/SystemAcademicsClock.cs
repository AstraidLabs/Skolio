using Skolio.Academics.Application.Abstractions;

namespace Skolio.Academics.Infrastructure.Services;

public sealed class SystemAcademicsClock : IAcademicsClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
