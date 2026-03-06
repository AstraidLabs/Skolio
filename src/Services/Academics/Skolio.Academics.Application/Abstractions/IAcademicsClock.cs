namespace Skolio.Academics.Application.Abstractions;

public interface IAcademicsClock
{
    DateTimeOffset UtcNow { get; }
}
