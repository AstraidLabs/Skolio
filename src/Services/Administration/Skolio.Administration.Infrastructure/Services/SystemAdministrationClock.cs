using Skolio.Administration.Application.Abstractions;
namespace Skolio.Administration.Infrastructure.Services;
public sealed class SystemAdministrationClock : IAdministrationClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
