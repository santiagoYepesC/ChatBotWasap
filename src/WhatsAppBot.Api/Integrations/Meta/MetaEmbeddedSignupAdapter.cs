using Microsoft.Extensions.Options;
using System.Text.Json;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Infrastructure.Options;
using WhatsAppBot.Api.Models.Contracts;

namespace WhatsAppBot.Api.Integrations.Meta;

public sealed class MetaEmbeddedSignupAdapter(
    IMetaCloudApiClient metaClient,
    ISecretStore secretStore,
    IWhatsAppIntegrationRepository integrationRepository,
    IOptions<MetaOptions> options) : IMetaEmbeddedSignupAdapter
{
    public async Task<MetaSignupResult> CompleteAsync(
        string authorizationCode, string wabaId, string phoneNumberId, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.AppId) ||
                string.IsNullOrWhiteSpace(settings.AppSecret) ||
                string.IsNullOrWhiteSpace(settings.EmbeddedSignupConfigId) ||
                string.IsNullOrWhiteSpace(settings.VerifyToken))
            {
                throw new MetaConfigurationException("Meta Embedded Signup server configuration is incomplete.");
            }

            if (string.IsNullOrWhiteSpace(authorizationCode) ||
                string.IsNullOrWhiteSpace(wabaId) ||
                string.IsNullOrWhiteSpace(phoneNumberId))
            {
                throw new MetaConfigurationException("Meta Embedded Signup returned incomplete asset identifiers.");
            }

            var token = await metaClient.ExchangeAuthorizationCodeAsync(authorizationCode, cancellationToken);
            if (!await metaClient.PhoneNumberBelongsToWabaAsync(token, wabaId, phoneNumberId, cancellationToken))
            {
                throw new MetaConfigurationException("The selected phone number is not part of the selected WABA.");
            }

            var phone = await metaClient.GetPhoneNumberAsync(token, phoneNumberId, cancellationToken);
            await metaClient.RegisterPhoneNumberAsync(token, phoneNumberId, cancellationToken);
            await metaClient.SubscribeWabaAsync(token, wabaId, cancellationToken);

            var integration = await integrationRepository.GetCurrentAsync(cancellationToken);
            string tokenReference;
            try
            {
                tokenReference = await secretStore.StoreAsync(
                    "whatsapp-business-access-token", token, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                throw new ExternalDependencyException("SecretStoreUnavailable");
            }
            catch (UnauthorizedAccessException)
            {
                throw new ExternalDependencyException("SecretStoreUnavailable");
            }
            catch (IOException)
            {
                throw new ExternalDependencyException("SecretStoreUnavailable");
            }
            return new MetaSignupResult(
                integration.IntegrationId,
                wabaId,
                phoneNumberId,
                phone.DisplayPhoneNumber,
                phone.VerifiedName,
                tokenReference,
                "Meta:AppSecret",
                "Meta:VerifyToken");
        }
        catch (MetaApiException exception)
        {
            throw new ExternalDependencyException(exception.SafeCode, exception.IsTransient);
        }
        catch (MetaConfigurationException)
        {
            throw new ExternalDependencyException("MetaConfigurationMissing");
        }
        catch (JsonException)
        {
            throw new ExternalDependencyException("MetaInvalidResponse");
        }
    }
}
