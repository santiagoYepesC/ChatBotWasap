using WhatsAppBot.Api.Business.Extensions;
using WhatsAppBot.Api.Infrastructure.Extensions;
using WhatsAppBot.Api.Integrations.Extensions;
using WhatsAppBot.Api.Persistence.Extensions;

namespace WhatsAppBot.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) =>
        BusinessServiceCollectionExtensions.AddBusiness(services);

    public static IServiceCollection AddPersistence(this IServiceCollection services) =>
        PersistenceServiceCollectionExtensions.AddPersistence(services);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        InfrastructureServiceCollectionExtensions.AddInfrastructure(services, configuration, environment);

    public static IServiceCollection AddIntegrations(this IServiceCollection services) =>
        IntegrationsServiceCollectionExtensions.AddIntegrations(services);
}
