namespace Skolio.Administration.Application.Abstractions;

public interface IAdministrationClock
{
    DateTimeOffset UtcNow { get; }
}
