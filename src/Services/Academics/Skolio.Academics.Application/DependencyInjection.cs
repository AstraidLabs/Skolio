using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.Academics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAcademicsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssemblyMarker>());

        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.Scan(typeof(AssemblyMarker).Assembly);
        services.AddSingleton(mapsterConfig);

        return services;
    }
}
