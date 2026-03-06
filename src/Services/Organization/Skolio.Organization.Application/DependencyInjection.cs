using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.Organization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssemblyMarker>());

        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.Scan(typeof(AssemblyMarker).Assembly);
        services.AddSingleton(mapsterConfig);

        return services;
    }
}
