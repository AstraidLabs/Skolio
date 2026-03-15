using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.ServiceDefaults.Authentication.ClientCredentials;

public static class SkolioServiceAuthenticationExtensions
{
    public static IHttpClientBuilder AddSkolioServiceClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddOptions<ServiceClientOptions>()
            .Bind(configuration.GetSection(configSectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<ServiceTokenDelegatingHandler>();

        return services.AddHttpClient<TClient, TImplementation>(httpClient =>
            {
                configureClient?.Invoke(httpClient);
            })
            .AddHttpMessageHandler<ServiceTokenDelegatingHandler>();
    }
}
