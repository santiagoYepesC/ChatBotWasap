using Microsoft.Extensions.Options;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Infrastructure.Options;
using WhatsAppBot.Api.Integrations.Meta;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Api.Models.Entities;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Integrations.Tests;

public sealed class MetaEmbeddedSignupTests
{
    [Fact]
    public async Task CompleteSignup_ExchangesAndRegistersServerSideThenReturnsOnlySecretReference()
    {
        var cloud = new FakeMetaCloudApiClient();
        var secretStore = new FakeSecretStore();
        var adapter = CreateAdapter(cloud, secretStore);

        var result = await adapter.CompleteAsync(
            "single-use-code", "waba-1", "phone-1", CancellationToken.None);

        Assert.Equal("waba-1", result.WabaId);
        Assert.Equal("phone-1", result.PhoneNumberId);
        Assert.Equal("secret-ref-123", result.AccessTokenSecretRef);
        Assert.DoesNotContain("access-token-value", System.Text.Json.JsonSerializer.Serialize(result));
        Assert.Equal(["exchange", "verify-waba", "lookup-phone", "register-phone", "subscribe-waba"], cloud.Calls);
        Assert.Equal(1, secretStore.StoreCalls);
    }

    [Fact]
    public async Task CompleteSignup_RejectsPhoneThatDoesNotBelongToSelectedWaba()
    {
        var cloud = new FakeMetaCloudApiClient { PhoneBelongsToWaba = false };
        var secretStore = new FakeSecretStore();
        var adapter = CreateAdapter(cloud, secretStore);

        var error = await Assert.ThrowsAsync<ExternalDependencyException>(() =>
            adapter.CompleteAsync("single-use-code", "other-waba", "phone-1", CancellationToken.None));

        Assert.Equal("MetaConfigurationMissing", error.SafeCode);
        Assert.DoesNotContain("register-phone", cloud.Calls);
        Assert.DoesNotContain("subscribe-waba", cloud.Calls);
        Assert.Equal(0, secretStore.StoreCalls);
    }

    [Fact]
    public async Task CompleteSignup_MapsProviderFailuresToSafeProviderNeutralErrors()
    {
        var cloud = new FakeMetaCloudApiClient { FailExchange = true };
        var adapter = CreateAdapter(cloud, new FakeSecretStore());

        var error = await Assert.ThrowsAsync<ExternalDependencyException>(() =>
            adapter.CompleteAsync("single-use-code", "waba-1", "phone-1", CancellationToken.None));

        Assert.Equal("MetaAuthorizationFailed", error.SafeCode);
    }

    private static MetaEmbeddedSignupAdapter CreateAdapter(
        FakeMetaCloudApiClient cloud, FakeSecretStore secretStore) =>
        new(
            cloud,
            secretStore,
            new FakeIntegrationRepository(),
            Options.Create(new MetaOptions
            {
                AppId = "public-app",
                AppSecret = "secret",
                VerifyToken = "verify-secret",
                EmbeddedSignupConfigId = "config"
            }));

    private sealed class FakeMetaCloudApiClient : IMetaCloudApiClient
    {
        public List<string> Calls { get; } = [];
        public bool PhoneBelongsToWaba { get; init; } = true;
        public bool FailExchange { get; init; }

        public Task<string> ExchangeAuthorizationCodeAsync(string code, CancellationToken cancellationToken)
        {
            Calls.Add("exchange");
            return FailExchange
                ? throw new MetaApiException("MetaAuthorizationFailed", false)
                : Task.FromResult("access-token-value");
        }

        public Task<MetaPhoneNumberDetails> GetPhoneNumberAsync(
            string accessToken, string phoneNumberId, CancellationToken cancellationToken)
        {
            Calls.Add("lookup-phone");
            return Task.FromResult(new MetaPhoneNumberDetails("+15551234567", "Test business"));
        }

        public Task<bool> PhoneNumberBelongsToWabaAsync(
            string accessToken, string wabaId, string phoneNumberId, CancellationToken cancellationToken)
        {
            Calls.Add("verify-waba");
            return Task.FromResult(PhoneBelongsToWaba);
        }

        public Task RegisterPhoneNumberAsync(
            string accessToken, string phoneNumberId, CancellationToken cancellationToken)
        {
            Calls.Add("register-phone");
            return Task.CompletedTask;
        }

        public Task SubscribeWabaAsync(string accessToken, string wabaId, CancellationToken cancellationToken)
        {
            Calls.Add("subscribe-waba");
            return Task.CompletedTask;
        }

        public Task<MetaMessageSendResult> SendTextAsync(
            string accessToken, string phoneNumberId, string recipient, string text,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeSecretStore : ISecretStore
    {
        public int StoreCalls { get; private set; }
        public Task<string> StoreAsync(string secretName, string secretValue, CancellationToken cancellationToken)
        {
            StoreCalls++;
            Assert.Equal("access-token-value", secretValue);
            return Task.FromResult("secret-ref-123");
        }
        public ValueTask<string?> ResolveAsync(string secretReference, CancellationToken cancellationToken) =>
            ValueTask.FromResult<string?>(null);
        public Task DeleteAsync(string secretReference, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeIntegrationRepository : IWhatsAppIntegrationRepository
    {
        private readonly WhatsAppIntegration integration = new(
            Guid.NewGuid(), null, null, null, null, ConnectionState.NotConnected, null, false);
        public Task<WhatsAppIntegration> GetCurrentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(integration);
        public Task SaveSignupResultAsync(MetaSignupResult result, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task SetConnectionStateAsync(
            Guid integrationId, ConnectionState state, string? safeErrorCode, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task DisconnectAsync(Guid integrationId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
