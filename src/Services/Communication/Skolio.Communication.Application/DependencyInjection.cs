using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.Communication.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunicationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssemblyMarker>());

        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.Scan(typeof(AssemblyMarker).Assembly);
        services.AddSingleton(mapsterConfig);

        return services;
    }
}
