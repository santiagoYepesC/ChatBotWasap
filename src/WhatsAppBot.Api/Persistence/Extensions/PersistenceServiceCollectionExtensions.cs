using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Persistence.Repositories;

namespace WhatsAppBot.Api.Persistence.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<StoredProcedureExecutor>();
        services.AddScoped<IAdministratorRepository, AdminRepository>();
        services.AddScoped<IBotConfigurationRepository, BotConfigurationRepository>();
        services.AddScoped<IFrequentResponseRepository, FrequentResponseRepository>();
        return services;
    }
}
