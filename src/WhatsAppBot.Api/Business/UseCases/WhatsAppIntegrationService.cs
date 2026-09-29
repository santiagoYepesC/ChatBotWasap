using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Api.Models.Entities;
using WhatsAppBot.Shared.Enums;
using System.IO;

namespace WhatsAppBot.Api.Business.UseCases;

public sealed class WhatsAppIntegrationService(
    IWhatsAppIntegrationRepository repository,
    IMetaEmbeddedSignupAdapter signupAdapter,
    ISecretStore secretStore) : IWhatsAppIntegrationService
{
    public Task<WhatsAppIntegration> GetCurrentAsync(CancellationToken cancellationToken) =>
        repository.GetCurrentAsync(cancellationToken);

    public async Task<WhatsAppIntegration> CompleteSignupAsync(
        string authorizationCode,
        string wabaId,
        string phoneNumberId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode) ||
            string.IsNullOrWhiteSpace(wabaId) ||
            string.IsNullOrWhiteSpace(phoneNumberId))
        {
            throw new BusinessValidationException("Meta signup must include its single-use code and asset identifiers.");
        }

        var integration = await repository.GetCurrentAsync(cancellationToken);
        if (integration.ConnectionState == ConnectionState.Connected)
        {
            throw new BusinessValidationException("Disconnect the current WhatsApp number before connecting another.");
        }

        await repository.SetConnectionStateAsync(
            integration.IntegrationId, ConnectionState.Configuring, null, cancellationToken);
        try
        {
            var result = await signupAdapter.CompleteAsync(
                authorizationCode, wabaId, phoneNumberId, cancellationToken);
            await repository.SaveSignupResultAsync(result, cancellationToken);
            return await repository.GetCurrentAsync(cancellationToken);
        }
        catch (ExternalDependencyException exception)
        {
            await repository.SetConnectionStateAsync(
                integration.IntegrationId, ConnectionState.Error, exception.SafeCode, cancellationToken);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var integration = await repository.GetCurrentAsync(cancellationToken);
        await repository.DisconnectAsync(integration.IntegrationId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(integration.AccessTokenSecretRef))
        {
            try
            {
                await secretStore.DeleteAsync(integration.AccessTokenSecretRef, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                throw new ExternalDependencyException("CredentialCleanupFailed");
            }
            catch (UnauthorizedAccessException)
            {
                throw new ExternalDependencyException("CredentialCleanupFailed");
            }
            catch (IOException)
            {
                throw new ExternalDependencyException("CredentialCleanupFailed");
            }
        }
    }
}
