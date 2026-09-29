using RestSharp;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Integrations.Meta;

namespace WhatsAppBot.Api.Integrations.Extensions;

public static class IntegrationsServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<IRestClient>(_ => new RestClient(new RestClientOptions("https://graph.facebook.com/")
        {
            Timeout = TimeSpan.FromSeconds(30)
        }));
        services.AddScoped<IMetaCloudApiClient, MetaCloudApiClient>();
        services.AddScoped<IMetaEmbeddedSignupAdapter, MetaEmbeddedSignupAdapter>();
        services.AddScoped<IMetaWhatsAppClient, MetaMessageSender>();
        services.AddSingleton<IMetaWebhookSignatureValidator, MetaWebhookSignatureValidator>();
        services.AddSingleton<IMetaWebhookChallengeValidator, MetaWebhookChallengeValidator>();
        services.AddSingleton<IMetaWebhookEventParser, MetaWebhookEventParser>();
        return services;
    }
}
