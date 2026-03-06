using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssemblyMarker>());

        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.Scan(typeof(AssemblyMarker).Assembly);
        services.AddSingleton(mapsterConfig);

        return services;
    }
}
